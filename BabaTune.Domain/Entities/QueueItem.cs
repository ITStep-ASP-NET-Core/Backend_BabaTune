
namespace BabaTune.Domain.Entities
{
	public class QueueItem
	{
		public Guid Id { get; set; }
		public Guid RoomId { get; set; }
		public Room? Room { get; set; }
		public Guid SongId { get; set; }
		public Song? Song { get; set; }
		public DateTime AddedAt { get; set; }
	}
}
