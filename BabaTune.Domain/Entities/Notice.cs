
namespace BabaTune.Domain.Entities
{
    public enum NoticeType
    {
        Personal,
        Server
    }

    public class Notice : IAuditable
    {
        public Guid Id { get; set; }
        public NoticeType Type { get; set; }
        public string Title { get; set; } = null!;
        public string? Text { get; set; }
        public bool IsRead { get; set; } = false;
        public Guid? SenderId { get; set; }
        public User? Sender { get; set; }
        public Guid RecipientId { get; set; }
        public User Recipient { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public string? Url { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}