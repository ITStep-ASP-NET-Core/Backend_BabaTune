
namespace BabaTune.Domain.Entities
{
    public enum PlaylistType
    {
        Liked,
        Custom
    }

    public class Playlist : IAuditable
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public PlaylistType Type { get; set; } = PlaylistType.Custom;
		public string? ImageUrl { get; set; }
		public Guid UserId { get; set; }
        public User User { get; set; } = null!;
		public ICollection<PlaylistItem> Items { get; set; } = [];
		public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
		public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	}
}