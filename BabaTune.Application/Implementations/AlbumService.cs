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
			var imageUrl = DefaultAlbumImageUrl;
			if (albumDto.ImageFile is not null)
			{
				await using var imageStream = albumDto.ImageFile.OpenReadStream();
				imageUrl = await _uow.Storage.UploadAsync(imageStream, albumDto.ImageFile.FileName, albumDto.ImageFile.ContentType, "albums/covers");
			}

			var album = AlbumMapper.ToEntity(albumDto, userId, imageUrl);

			await _uow.Albums.AddAsync(album);
			await _uow.SaveChangesAsync();

			return Result.Ok();
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

		public async Task<Result> DeleteAsync ( Guid albumId, Guid userId )
		{
			var album = await _uow.Albums.GetByIdAsync(albumId);
			if (album is null)
				return Result.Fail("Album not found.");

			if (album.UserId != userId)
				return Result.Fail("You are not the author of this album.");

			if (album.ImageUrl is not null && album.ImageUrl != DefaultAlbumImageUrl)
				await _uow.Storage.DeleteAsync(album.ImageUrl);

			_uow.Albums.Delete(album);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}
	}
}
