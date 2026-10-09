
namespace BabaTune.Domain.Entities
{
	public enum ChatType
	{
		Personal,
		Room
	}
	public class Chat : IAuditable
	{
		public Guid Id { get; set; }
		public ICollection<Message> Messages { get; set; } = [];
		public ChatType Type { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}
}
