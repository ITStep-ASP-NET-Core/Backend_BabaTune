using BabaTune.Application.DTO.Users;

namespace BabaTune.Application.DTO.Rooms
{
	public class RoomDto : RoomCardDto
	{
		public Guid ChatId { get; set; }
		public int PlaybackPositionMs { get; set; }
		public List<UserSummaryDto> Members { get; set; } = [];
		public List<QueueItemDto> Queue { get; set; } = [];
	}
}
