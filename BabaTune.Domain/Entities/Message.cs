
namespace BabaTune.Domain.Entities
{
	public enum MessageType
	{
		Text,
		Song
	}

	public abstract class Message : IAuditable
	{
		public Guid Id { get; set; }
		public Guid ChatId { get; set; }
		public Chat? Chat { get; set; }
		public Guid UserId { get; set; }
		public User? User { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}

	public class TextMessage : Message
	{
		public string Text { get; set; } = string.Empty;
	}

	public class SongMessage : Message
	{
		public Guid SongId { get; set; }
		public Song? Song { get; set; }
	}
}
