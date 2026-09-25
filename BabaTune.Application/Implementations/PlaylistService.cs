using BabaTune.Application.Common;
using BabaTune.Application.DTO.Playlists;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Mappers;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.Implementations
{
	public class PlaylistService : IPlaylistService
	{
		private const string DefaultPlaylistImageUrl = "https://storage.babatune.app/defaults/playlist-cover.png";

		private readonly IListenHistoryService _listenHistoryService;
		private readonly IUnitOfWork _uow;

		public PlaylistService ( IUnitOfWork uow, IListenHistoryService listenHistoryService )
		{
			_uow = uow;
			_listenHistoryService = listenHistoryService;
		}

		public async Task<PlaylistDto?> GetByIdAsync ( Guid playlistId )
		{
			var playlist = await _uow.Playlists.GetByIdAsync(playlistId);
			if(playlist is null)
				return null;

			var songsCount = await _uow.Playlists.GetSongsCountAsync(playlistId);
			var totalDuration = await _uow.Playlists.GetTotalDurationAsync(playlistId);

			return PlaylistMapper.ToDto(playlist, songsCount, totalDuration);
		}

		public async Task<PlaylistDto?> GetLikedAsync ( Guid userId )
		{
			var playlist = await _uow.Playlists.GetLikedPlaylistAsync(userId);
			if(playlist is null)
				return null;

			var songsCount = await _uow.Playlists.GetSongsCountAsync(playlist.Id);
			var totalDuration = await _uow.Playlists.GetTotalDurationAsync(playlist.Id);

			return PlaylistMapper.ToDto(playlist, songsCount, totalDuration);
		}

		public async Task<PagedResult<PlaylistSidebarDto>> GetByUserAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var playlists = await _uow.Playlists.GetCustomByUserAsync(userId, pageNumber, pageSize);

			return new PagedResult<PlaylistSidebarDto>
			{
				Items = playlists.Items.Select(PlaylistMapper.ToSidebarDto).ToList(),
				PageNumber = playlists.PageNumber,
				PageSize = playlists.PageSize,
				TotalCount = playlists.TotalCount
			};
		}

		public async Task<Result> CreateAsync ( CreatePlaylistDto playlistDto, Guid userId )
		{
			var imageUrl = DefaultPlaylistImageUrl;
			if(playlistDto.ImageFile is not null)
			{
				await using var imageStream = playlistDto.ImageFile.OpenReadStream();
				imageUrl = await _uow.Storage.UploadAsync(imageStream, playlistDto.ImageFile.FileName, playlistDto.ImageFile.ContentType, "playlists/covers");
			}

			var playlist = PlaylistMapper.ToEntity(playlistDto, userId, imageUrl);
			playlist.CreatedAt = DateTime.UtcNow;
			playlist.UpdatedAt = DateTime.UtcNow;

			await _uow.Playlists.AddAsync(playlist);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> UpdateInfoAsync ( Guid playlistId, Guid userId, UpdatePlaylistDto playlistDto )
		{
			var playlist = await _uow.Playlists.GetByIdAsync(playlistId);
			if(playlist is null)
				return Result.Fail("Playlist not found.");

			if(playlist.UserId != userId)
				return Result.Fail("You are not the owner of this playlist.");

			if(playlist.Type == PlaylistType.Liked)
				return Result.Fail("Liked playlist cannot be renamed.");

			playlist.Name = playlistDto.Name ?? playlist.Name;
			playlist.UpdatedAt = DateTime.UtcNow;

			_uow.Playlists.Update(playlist);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> UpdateImageAsync ( Guid playlistId, Guid userId, IFormFile? imageFile )
		{
			var playlist = await _uow.Playlists.GetByIdAsync(playlistId);
			if(playlist is null)
				return Result.Fail("Playlist not found.");

			if(playlist.UserId != userId)
				return Result.Fail("You are not the owner of this playlist.");

			if(playlist.ImageUrl is not null && playlist.ImageUrl != DefaultPlaylistImageUrl)
				await _uow.Storage.DeleteAsync(playlist.ImageUrl);

			if(imageFile is not null)
			{
				await using var imageStream = imageFile.OpenReadStream();
				playlist.ImageUrl = await _uow.Storage.UploadAsync(imageStream, imageFile.FileName, imageFile.ContentType, "playlists/covers");
			}
			else
			{
				playlist.ImageUrl = DefaultPlaylistImageUrl;
			}

			playlist.UpdatedAt = DateTime.UtcNow;

			_uow.Playlists.Update(playlist);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> DeleteAsync ( Guid playlistId, Guid userId )
		{
			var playlist = await _uow.Playlists.GetByIdAsync(playlistId);
			if(playlist is null)
				return Result.Fail("Playlist not found.");

			if(playlist.UserId != userId)
				return Result.Fail("You are not the owner of this playlist.");

			if(playlist.Type == PlaylistType.Liked)
				return Result.Fail("Liked playlist cannot be deleted.");

			if(playlist.ImageUrl is not null && playlist.ImageUrl != DefaultPlaylistImageUrl)
				await _uow.Storage.DeleteAsync(playlist.ImageUrl);

			_uow.Playlists.Delete(playlist);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> AddSongAsync ( Guid playlistId, Guid userId, Guid songId )
		{
			var playlist = await _uow.Playlists.GetByIdAsync(playlistId);
			if(playlist is null)
				return Result.Fail("Playlist not found.");

			if(playlist.UserId != userId)
				return Result.Fail("You are not the owner of this playlist.");

			var song = await _uow.Songs.GetByIdAsync(songId);
			if(song is null)
				return Result.Fail("Song not found.");

			if(playlist.Type == PlaylistType.Liked)
				await _listenHistoryService.SyncLikeAsync(playlist.UserId, songId, true);

			await _uow.Playlists.AddSongAsync(playlistId, songId);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> RemoveSongAsync ( Guid playlistId, Guid userId, Guid songId )
		{
			var playlist = await _uow.Playlists.GetByIdAsync(playlistId);
			if(playlist is null)
				return Result.Fail("Playlist not found.");

			if(playlist.UserId != userId)
				return Result.Fail("You are not the owner of this playlist.");

			if(playlist.Type == PlaylistType.Liked)
				await _listenHistoryService.SyncLikeAsync(playlist.UserId, songId, false);

			await _uow.Playlists.RemoveSongAsync(playlistId, songId);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> MoveSongAsync ( Guid playlistId, Guid userId, Guid songId, int newPosition )
		{
			var playlist = await _uow.Playlists.GetByIdAsync(playlistId);
			if(playlist is null)
				return Result.Fail("Playlist not found.");

			if(playlist.UserId != userId)
				return Result.Fail("You are not the owner of this playlist.");

			var contains = await _uow.Playlists.ContainsSongAsync(playlistId, songId);
			if(!contains)
				return Result.Fail("Song is not in this playlist.");

			if(newPosition < 1)
				return Result.Fail("Position must be at least 1.");

			await _uow.Playlists.MoveSongAsync(playlistId, songId, newPosition);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}
	}
}