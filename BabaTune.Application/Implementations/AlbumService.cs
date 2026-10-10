using BabaTune.Application.Common;
using BabaTune.Application.DTO.Albums;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Mappers;
using BabaTune.Domain.Common;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.Implementations
{
	public class AlbumService : IAlbumService
	{
		private const string DefaultAlbumImageUrl = "https://storage.babatune.app/defaults/album-cover.png";
		private const int MaxAlbumSongs = 100;

		private readonly IUnitOfWork _uow;

		public AlbumService ( IUnitOfWork uow )
		{
			_uow = uow;
		}

		public async Task<AlbumDto?> GetByIdAsync ( Guid albumId )
		{
			var album = await _uow.Albums.GetByIdAsync(albumId);
			if (album is null)
				return null;

			return AlbumMapper.ToDto(album);
		}

		public async Task<PagedResult<AlbumCardDto>> GetByAuthorAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var albums = await _uow.Albums.GetByAuthorAsync(userId, pageNumber, pageSize);
			return new()
			{
				Items = albums.Items.Select(AlbumMapper.ToCardDto).ToList(),
				PageNumber = albums.PageNumber,
				PageSize = albums.PageSize,
				TotalCount = albums.TotalCount
			};
		}

		public async Task<PagedResult<AlbumCardDto>> SearchAsync ( string query, int pageNumber, int pageSize )
		{
			var albums = await _uow.Albums.SearchAsync(query, pageNumber, pageSize);
			return new()
			{
				Items = albums.Items.Select(AlbumMapper.ToCardDto).ToList(),
				PageNumber = albums.PageNumber,
				PageSize = albums.PageSize,
				TotalCount = albums.TotalCount
			};
		}

		public async Task<Result> CreateAsync ( CreateAlbumDto albumDto, Guid userId )
		{
			var songIds = albumDto.SongIds?.Distinct().ToList() ?? [];
			if (songIds.Count == 0)
				return Result.Fail("Album must contain at least one song.");

			var validation = await ValidateNewSongsAsync(userId, songIds, 0);
			if (!validation.Success)
				return validation;

			var imageUrl = DefaultAlbumImageUrl;

			try
			{
				if (albumDto.ImageFile is not null)
				{
					await using var imageStream = albumDto.ImageFile.OpenReadStream();
					imageUrl = await _uow.Storage.UploadAsync(imageStream, albumDto.ImageFile.FileName, albumDto.ImageFile.ContentType, "albums/covers");
				}

				var album = AlbumMapper.ToEntity(albumDto, userId, imageUrl);

				await _uow.Albums.AddAsync(album);
				await _uow.Albums.AddSongsAsync(album.Id, songIds);
				await _uow.SaveChangesAsync();

				return Result.Ok();
			}
			catch
			{
				if (imageUrl != DefaultAlbumImageUrl)
					await DeleteQuietlyAsync(imageUrl);

				throw;
			}
		}

		public async Task<Result> UpdateInfoAsync ( Guid albumId, Guid userId, UpdateAlbumDto albumDto )
		{
			var album = await _uow.Albums.GetByIdAsync(albumId);
			if (album is null)
				return Result.Fail("Album not found.");

			if (album.UserId != userId)
				return Result.Fail("You are not the author of this album.");

			album.Name = albumDto.Name ?? album.Name;
			album.Description = albumDto.Description ?? album.Description;
			album.UpdatedAt = DateTime.UtcNow;

			_uow.Albums.Update(album);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> UpdateImageAsync ( Guid albumId, Guid userId, IFormFile? imageFile )
		{
			var album = await _uow.Albums.GetByIdAsync(albumId);
			if (album is null)
				return Result.Fail("Album not found.");

			if (album.UserId != userId)
				return Result.Fail("You are not the author of this album.");

			if (album.ImageUrl is not null && album.ImageUrl != DefaultAlbumImageUrl)
				await _uow.Storage.DeleteAsync(album.ImageUrl);

			if (imageFile is not null)
			{
				await using var imageStream = imageFile.OpenReadStream();
				album.ImageUrl = await _uow.Storage.UploadAsync(imageStream, imageFile.FileName, imageFile.ContentType, "albums/covers");
			}
			else
			{
				album.ImageUrl = DefaultAlbumImageUrl;
			}

			album.UpdatedAt = DateTime.UtcNow;

			_uow.Albums.Update(album);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> AddSongsAsync ( Guid albumId, Guid userId, ICollection<Guid> songIds )
		{
			var album = await _uow.Albums.GetByIdAsync(albumId);
			if (album is null)
				return Result.Fail("Album not found.");

			if (album.UserId != userId)
				return Result.Fail("You are not the author of this album.");

			var ids = songIds?.Distinct().ToList() ?? [];
			if (ids.Count == 0)
				return Result.Fail("No songs provided.");

			var currentCount = await _uow.Albums.GetSongsCountAsync(albumId);

			var validation = await ValidateNewSongsAsync(userId, ids, currentCount);
			if (!validation.Success)
				return validation;

			await _uow.Albums.AddSongsAsync(albumId, ids);

			album.UpdatedAt = DateTime.UtcNow;
			_uow.Albums.Update(album);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> AddSongAsync ( Guid albumId, Guid userId, Guid songId )
		{
			return await AddSongsAsync(albumId, userId, [songId]);
		}

		public async Task<Result> RemoveSongAsync ( Guid albumId, Guid userId, Guid songId )
		{
			var album = await _uow.Albums.GetByIdAsync(albumId);
			if (album is null)
				return Result.Fail("Album not found.");

			if (album.UserId != userId)
				return Result.Fail("You are not the author of this album.");

			if (!await _uow.Albums.ContainsSongAsync(albumId, songId))
				return Result.Fail("Song is not in this album.");

			if (await _uow.Albums.GetSongsCountAsync(albumId) <= 1)
				return Result.Fail("Album must contain at least one song.");

			await _uow.Albums.RemoveSongAsync(albumId, songId);

			album.UpdatedAt = DateTime.UtcNow;
			_uow.Albums.Update(album);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> DeleteAsync ( Guid albumId, Guid userId, bool IsAdmin )
		{
			var album = await _uow.Albums.GetByIdAsync(albumId);
			if (album is null)
				return Result.Fail("Album not found.");

			if (album.UserId != userId && !IsAdmin)
				return Result.Fail("You are not the author of this album.");

			if (album.ImageUrl is not null && album.ImageUrl != DefaultAlbumImageUrl)
				await _uow.Storage.DeleteAsync(album.ImageUrl);

			_uow.Albums.Delete(album);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		private async Task<Result> ValidateNewSongsAsync ( Guid userId, ICollection<Guid> songIds, int currentCount )
		{
			if(currentCount + songIds.Count > MaxAlbumSongs)
				return Result.Fail("Album is full.");

			if (await _uow.Albums.CountOwnedSongsAsync(userId, songIds) != songIds.Count)
				return Result.Fail("You are not the author of these songs.");

			if (await _uow.Albums.AnySongInAlbumAsync(songIds))
				return Result.Fail("Song is already in an album.");

			return Result.Ok();
		}

		private async Task DeleteQuietlyAsync ( string url )
		{
			try
			{
				await _uow.Storage.DeleteAsync(url);
			}
			catch
			{
			}
		}
	}
}
