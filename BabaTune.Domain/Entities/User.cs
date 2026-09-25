
namespace BabaTune.Domain.Entities
{
    public class User : IAuditable
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public bool IsChecked { get; set; } = false;
        public string? AvatarUrl { get; set; }
		public ICollection<Subscribe> Subscriptions { get; set; } = [];
		public ICollection<Subscribe> Subscribers { get; set; } = [];
		public ICollection<Playlist> Playlists { get; set; } = [];
		public ICollection<Song> Songs { get; set; } = [];
        public ICollection<Album> Albums { get; set; } = [];
        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
        public ICollection<ListenHistory> ListenHistory { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}