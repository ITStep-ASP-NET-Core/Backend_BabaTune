using BabaTune.Application.Common;
using BabaTune.Application.DTO.Songs;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Mappers;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.Implementations
{
	public class SongService : ISongService
	{
		private const string DefaultSongImageUrl = "https://storage.babatune.app/defaults/song-cover.png";

		private readonly IUnitOfWork _uow;

		public SongService ( IUnitOfWork uow )
		{
			_uow = uow;
		}

		public async Task<PagedResult<SongDto>> GetTopAsync ( TopPeriod period, int pageNumber, int pageSize, Guid? currentUserId )
		{
			var from = period switch
			{
				TopPeriod.Day => DateTime.UtcNow.AddDays(-1),
				TopPeriod.Week => DateTime.UtcNow.AddDays(-7),
				TopPeriod.Month => DateTime.UtcNow.AddMonths(-1),
				_ => throw new ArgumentOutOfRangeException(nameof(period))
			};

			var topSongIds = await _uow.ListenHistories.GetTopSongIdsAsync(from, pageNumber, pageSize);
			var songsById = (await _uow.Songs.GetByIdsAsync(topSongIds.Items.ToHashSet())).ToDictionary(s => s.Id);

			var ordered = topSongIds.Items
				.Where(songsById.ContainsKey)
				.Select(id => songsById[id])
				.ToList();

			return await ToDtoPageAsync(new PagedResult<Song>
			{
				Items = ordered,
				PageNumber = pageNumber,
				PageSize = pageSize,
				TotalCount = topSongIds.TotalCount
			}, currentUserId);
		}

		public async Task<SongDto?> GetByIdAsync ( Guid songId, Guid? currentUserId )
		{
			var song = await _uow.Songs.GetWithAllAsync(songId);
			if(song is null)
				return null;

			bool isLiked = false;

			if(currentUserId is not null)
				isLiked = (await _uow.Playlists.GetLikedSongIdsAsync((Guid)currentUserId, [song.Id])).Contains(song.Id);

			return SongMapper.ToDto(song, isLiked);
		}

		public async Task<PagedResult<SongDto>> GetByAuthorAsync ( Guid userId, int pageNumber, int pageSize, Guid? currentUserId )
		{
			var songs = await _uow.Songs.GetByAuthorAsync(userId, pageNumber, pageSize);
			return await ToDtoPageAsync(songs, currentUserId);
		}

		public async Task<PagedResult<SongDto>> GetByFiltersAsync ( SongFilterDto filter, int pageNumber, int pageSize, Guid? currentUserId )
		{
			var songs = await _uow.Songs.GetByFiltersAsync(
				filter.SearchQuery,
				filter.AuthorId,
				filter.AlbumId,
				filter.CategoryIds,
				filter.GenreIds,
				pageNumber,
				pageSize);

			return await ToDtoPageAsync(songs, currentUserId);
		}

		public async Task<PagedResult<SongDto>> GetByPlaylistAsync ( Guid playlistId, int pageNumber, int pageSize, Guid? currentUserId )
		{
			var playlist = await _uow.Playlists.GetByIdAsync(playlistId);
			var items = await _uow.Playlists.GetItemsPagedAsync(playlistId, pageNumber, pageSize);

			var isOwnLikedPlaylist = playlist is not null
				&& playlist.Type == PlaylistType.Liked
				&& currentUserId is not null
				&& playlist.UserId == currentUserId.Value;

			HashSet<Guid> likedSongIds;
			if(isOwnLikedPlaylist)
			{
				likedSongIds = items.Items.Select(i => i.SongId).ToHashSet();
			}
			else if(currentUserId is not null)
			{
				var candidateIds = items.Items.Select(i => i.SongId).ToList();
				likedSongIds = await _uow.Playlists.GetLikedSongIdsAsync(currentUserId.Value, candidateIds);
			}
			else
			{
				likedSongIds = [];
			}

			var songDtos = items.Items
				.Select(i => SongMapper.ToDto(i.Song, likedSongIds.Contains(i.SongId)))
				.ToList();

			return new PagedResult<SongDto>
			{
				Items = songDtos,
				PageNumber = items.PageNumber,
				PageSize = items.PageSize,
				TotalCount = items.TotalCount
			};
		}

		public async Task<PagedResult<SongDto>> GetByAlbumAsync ( Guid albumId, int pageNumber, int pageSize, Guid? currentUserId )
		{
			var songs = await _uow.Songs.GetByAlbumAsync(albumId, pageNumber, pageSize);
			return await ToDtoPageAsync(songs, currentUserId);
		}

		public async Task<Result> CreateAsync ( CreateSongDto songDto, Guid userId )
		{
			var uploadedUrls = new List<string>();

			try
			{
				await using var audioStream = songDto.AudioFile.OpenReadStream();
				var audioUrl = await _uow.Storage.UploadAsync(audioStream, songDto.AudioFile.FileName, songDto.AudioFile.ContentType, "songs/audio");
				uploadedUrls.Add(audioUrl);

				var imageUrl = DefaultSongImageUrl;
				if(songDto.ImageFile is not null)
				{
					await using var imageStream = songDto.ImageFile.OpenReadStream();
					imageUrl = await _uow.Storage.UploadAsync(imageStream, songDto.ImageFile.FileName, songDto.ImageFile.ContentType, "songs/covers");
					uploadedUrls.Add(imageUrl);
				}

				var song = SongMapper.ToEntity(songDto, userId, audioUrl, imageUrl);
				song.CreatedAt = DateTime.UtcNow;
				song.UpdatedAt = DateTime.UtcNow;

				await AttachCategoriesAndGenresAsync(song, songDto.CategoryIds, songDto.GenreIds);

				await _uow.Songs.AddAsync(song);
				await _uow.SaveChangesAsync();

				return Result.Ok();
			}
			catch
			{
				await DeleteQuietlyAsync(uploadedUrls.ToArray());
				throw;
			}
		}

		public async Task<Result> UpdateInfoAsync ( Guid songId, Guid userId, UpdateSongDto songDto )
		{
			var song = await _uow.Songs.GetWithAllAsync(songId);
			if(song is null)
				return Result.Fail("Song not found.");

			if(song.UserId != userId)
				return Result.Fail("You are not the author of this song.");

			song.Name = songDto.Name;
			song.Description = songDto.Description;
			song.UpdatedAt = DateTime.UtcNow;

			song.Categories.Clear();
			song.Genres.Clear();
			await AttachCategoriesAndGenresAsync(song, songDto.CategoryIds, songDto.GenreIds);

			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> UpdateImageAsync ( Guid songId, Guid userId, IFormFile? imageFile )
		{
			var song = await _uow.Songs.GetByIdAsync(songId);
			if(song is null)
				return Result.Fail("Song not found.");

			if(song.UserId != userId)
				return Result.Fail("You are not the author of this song.");

			var oldImageUrl = song.ImageUrl;
			string? uploadedUrl = null;
			var newImageUrl = DefaultSongImageUrl;

			if(imageFile is not null)
			{
				await using var imageStream = imageFile.OpenReadStream();
				uploadedUrl = await _uow.Storage.UploadAsync(imageStream, imageFile.FileName, imageFile.ContentType, "songs/covers");
				newImageUrl = uploadedUrl;
			}

			try
			{
				song.ImageUrl = newImageUrl;
				song.UpdatedAt = DateTime.UtcNow;

				_uow.Songs.Update(song);
				await _uow.SaveChangesAsync();
			}
			catch
			{
				await DeleteQuietlyAsync(uploadedUrl);
				throw;
			}

			if(oldImageUrl != DefaultSongImageUrl)
				await DeleteQuietlyAsync(oldImageUrl);

			return Result.Ok();
		}

		public async Task<Result> UpdateAudioAsync ( Guid songId, Guid userId, IFormFile audioFile )
		{
			var song = await _uow.Songs.GetByIdAsync(songId);
			if(song is null)
				return Result.Fail("Song not found.");

			if(song.UserId != userId)
				return Result.Fail("You are not the author of this song.");

			var oldAudioUrl = song.Url;

			await using var audioStream = audioFile.OpenReadStream();
			var newAudioUrl = await _uow.Storage.UploadAsync(audioStream, audioFile.FileName, audioFile.ContentType, "songs/audio");

			try
			{
				song.Url = newAudioUrl;
				song.UpdatedAt = DateTime.UtcNow;

				_uow.Songs.Update(song);
				await _uow.SaveChangesAsync();
			}
			catch
			{
				await DeleteQuietlyAsync(newAudioUrl);
				throw;
			}

			await DeleteQuietlyAsync(oldAudioUrl);

			return Result.Ok();
		}

		public async Task<Result> DeleteAsync ( Guid songId, Guid userId )
		{
			var song = await _uow.Songs.GetByIdAsync(songId);
			if(song is null)
				return Result.Fail("Song not found.");

			if(song.UserId != userId)
				return Result.Fail("You are not the author of this song.");

			var audioUrl = song.Url;
			var imageUrl = song.ImageUrl;

			_uow.Songs.Delete(song);
			await _uow.SaveChangesAsync();

			await DeleteQuietlyAsync(audioUrl, imageUrl != DefaultSongImageUrl ? imageUrl : null);

			return Result.Ok();
		}

		private async Task AttachCategoriesAndGenresAsync ( Song song, List<int> categoryIds, List<int> genreIds )
		{
			foreach(var categoryId in categoryIds)
			{
				var category = await _uow.Categories.GetByIdAsync(categoryId);
				if(category is not null)
					song.Categories.Add(category);
			}

			foreach(var genreId in genreIds)
			{
				var genre = await _uow.Genres.GetByIdAsync(genreId);
				if(genre is not null)
					song.Genres.Add(genre);
			}
		}

		private async Task DeleteQuietlyAsync ( params string?[] urls )
		{
			foreach(var url in urls)
			{
				if(string.IsNullOrEmpty(url))
					continue;

				try
				{
					await _uow.Storage.DeleteAsync(url);
				}
				catch
				{
				}
			}
		}

		private async Task<PagedResult<SongDto>> ToDtoPageAsync ( PagedResult<Song> songs, Guid? currentUserId )
		{
			var likedSongIds = currentUserId is null
				? new HashSet<Guid>()
				: await _uow.Playlists.GetLikedSongIdsAsync(currentUserId.Value, songs.Items.Select(s => s.Id).ToList());

			var items = songs.Items.Select(s => SongMapper.ToDto(s, likedSongIds.Contains(s.Id))).ToList();

			return new PagedResult<SongDto>
			{
				Items = items,
				PageNumber = songs.PageNumber,
				PageSize = songs.PageSize,
				TotalCount = songs.TotalCount
			};
		}
	}
}