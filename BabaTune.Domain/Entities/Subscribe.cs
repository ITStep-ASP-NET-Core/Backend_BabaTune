
namespace BabaTune.Domain.Entities
{
    public class Subscribe
	{
        public Guid Id { get; set; }
		public Guid SubscriberId { get; set; }
		public User Subscriber { get; set; } = null!;
		public Guid SubscribedToId { get; set; }
		public User SubscribedTo { get; set; } = null!;
		public DateTime SubscribedAt { get; set; }
	}
}