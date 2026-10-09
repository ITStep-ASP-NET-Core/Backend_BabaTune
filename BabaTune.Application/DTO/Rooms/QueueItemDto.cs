using BabaTune.Application.DTO.Songs;

namespace BabaTune.Application.DTO.Rooms
{
	public class QueueItemDto
	{
		public Guid Id { get; set; }
		public SongDto Song { get; set; } = null!;
		public DateTime AddedAt { get; set; }
	}
}
