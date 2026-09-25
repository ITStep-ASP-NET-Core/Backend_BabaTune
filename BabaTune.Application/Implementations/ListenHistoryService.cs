using BabaTune.Application.Common;
using BabaTune.Application.DTO.ListenHistory;
using BabaTune.Application.DTO.Songs;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Mappers;
using BabaTune.Domain.Common;
using BabaTune.Infrastructure.Interfaces;

namespace BabaTune.Application.Implementations
{
	public class ListenHistoryService : IListenHistoryService
	{
		private readonly IUnitOfWork _uow;

		public ListenHistoryService ( IUnitOfWork uow )
		{
			_uow = uow;
		}

		public async Task<Result> RecordAsync ( RecordListenHistoryDto historyDto )
		{
			var song = await _uow.Songs.GetByIdAsync(historyDto.SongId);
			if(song is null)
				return Result.Fail("Song not found.");

			var entry = await _uow.ListenHistories.GetEntryAsync(historyDto.UserId, historyDto.SongId);
			if(entry is null)
			{
				var likedSongs = await _uow.Playlists.GetLikedSongIdsAsync(historyDto.UserId, [historyDto.SongId]);
				entry = ListenHistoryMapper.ToEntity(historyDto, likedSongs.Contains(historyDto.SongId));
				await _uow.ListenHistories.AddAsync(entry);
			}
			else
			{
				entry.PlayedPercent = historyDto.PlayedPercent > entry.PlayedPercent ? historyDto.PlayedPercent : entry.PlayedPercent;
				entry.ListenedAt = DateTime.UtcNow;
			}

			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<PagedResult<SongDto>> GetHistoryAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var history = await _uow.ListenHistories.GetByUserAsync(userId, pageNumber, pageSize);

			if (history.Items.Count == 0)
				return new PagedResult<SongDto>
				{
					Items = [],
					PageNumber = history.PageNumber,
					PageSize = history.PageSize,
					TotalCount = history.TotalCount
				};

			var likedSongIds = await _uow.Playlists.GetLikedSongIdsAsync(userId, history.Items.Select(h => h.SongId).ToList());

			var items = history.Items
				.Select(h => SongMapper.ToDto(h.Song, likedSongIds.Contains(h.SongId)))
				.ToList();

			return new PagedResult<SongDto>
			{
				Items = items,
				PageNumber = history.PageNumber,
				PageSize = history.PageSize,
				TotalCount = history.TotalCount
			};
		}
		public async Task SyncLikeAsync ( Guid userId, Guid songId, bool isLiked )
		{
			var entry = await _uow.ListenHistories.GetEntryAsync(userId, songId);
			if(entry is null)
				return;

			entry.IsLiked = isLiked;
			_uow.ListenHistories.Update(entry);
		}
	}
}
