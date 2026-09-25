
namespace BabaTune.Domain.Entities
{
    public class RefreshToken : IAuditable
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        public string Token { get; set; } = null!;
		public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}