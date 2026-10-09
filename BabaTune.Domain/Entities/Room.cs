
namespace BabaTune.Domain.Entities
{
	public enum RoomType
	{
		Public,
		Private
	}

	public class Room : IAuditable
	{
		public Guid Id { get; set; }
		public Guid OwnerId { get; set; }
		public User? Owner { get; set; }
		public Guid ChatId { get; set; }
		public Chat? Chat { get; set; }
		public ICollection<User> Members { get; set; } = [];
		public ICollection<QueueItem> Queue { get; set; } = [];
		public RoomType Type { get; set; }
		public Guid? CurrentSongId { get; set; }
		public Song? CurrentSong { get; set; }
		public int PlaybackPositionMs { get; set; }
		public bool IsPlaying { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}
}
