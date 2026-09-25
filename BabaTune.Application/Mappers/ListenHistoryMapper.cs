using BabaTune.Application.DTO.ListenHistory;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.Mappers
{
	public class ListenHistoryMapper
	{
		public static ListenHistory ToEntity ( RecordListenHistoryDto historyDto, bool isLiked ) => new()
		{
			Id = Guid.NewGuid(),
			UserId = historyDto.UserId,
			SongId = historyDto.SongId,
			PlayedPercent = historyDto.PlayedPercent,
			IsLiked = isLiked,
			ListenedAt = DateTime.UtcNow,
		};
	}
}
