
namespace BabaTune.Domain.Entities
{
	public enum FriendshipStatus
	{
		Pending,
		Accepted,
		Blocked
	}

	public class Friendship : IAuditable
	{
		public Guid Id { get; set; }
		public Guid SenderId { get; set; }
		public User? Sender { get; set; }
		public Guid RecipientId { get; set; }
		public User? Recipient { get; set; }
		public Guid? ChatId { get; set; }
		public Chat? Chat { get; set; }
		public FriendshipStatus Status { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}
}
