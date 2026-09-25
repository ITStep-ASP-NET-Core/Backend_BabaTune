using BabaTune.Application.Common;
using BabaTune.Application.DTO.ListenHistory;
using BabaTune.Application.DTO.Songs;
using BabaTune.Domain.Common;

namespace BabaTune.Application.Interfaces
{
	public interface IListenHistoryService
	{
		Task<Result> RecordAsync ( RecordListenHistoryDto historyDto );
		Task<PagedResult<SongDto>> GetHistoryAsync ( Guid userId, int pageNumber, int pageSize );
		Task SyncLikeAsync ( Guid userId, Guid songId, bool isLiked );
	}
}
