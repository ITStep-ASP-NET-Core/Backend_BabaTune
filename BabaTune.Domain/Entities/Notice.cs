
namespace BabaTune.Domain.Entities
{
	public enum NoticeType
	{
		FriendRequest,
		Message,
		NewSong,
		NewAlbum,
		NewRoom,
		NewSubscription,
		Server
	}

	public abstract class Notice : IAuditable
	{
		public Guid Id { get; set; }
		public bool IsRead { get; set; } = false;
		public Guid RecipientId { get; set; }
		public User Recipient { get; set; } = null!;
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}

	public class FriendRequestNotice : Notice
	{
		public Guid SenderId { get; set; }
		public User? Sender { get; set; }
		public Guid FriendShipId { get; set; }
		public Friendship? FriendShip { get; set; }
	}

	public class MessageNotice : Notice
	{
		public Guid ChatId { get; set; }
		public Chat? Chat { get; set; }
		public string? Text { get; set; }
	}

	public class NewSongNotice : Notice
	{
		public Guid AuthorId { get; set; }
		public User? Author { get; set; }
		public Guid SongId { get; set; }
		public Song? Song { get; set; }
	}

	public class NewAlbumNotice : Notice
	{
		public Guid AuthorId { get; set; }
		public User? Author { get; set; }
		public Guid AlbumId { get; set; }
		public Album? Album { get; set; }
	}

	public class NewRoomNotice : Notice
	{
		public Guid OwnerId { get; set; }
		public User? Owner { get; set; }
		public Guid RoomId { get; set; }
		public Room? Room { get; set; }
	}

	public class NewSubscriptionNotice : Notice
	{
		public Guid SubscribeId { get; set; }
		public Subscribe? Subscribe { get; set; }
	}

	public class ServerNotice : Notice
	{
		public string? Text { get; set; }
		public string? ImageUrl { get; set; }
		public string? Url { get; set; }
	}
}