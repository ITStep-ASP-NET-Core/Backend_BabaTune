
namespace BabaTune.Domain.Entities
{
    public class Song : IAuditable
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public int Duration { get; set; }
        public Guid? AlbumId { get; set; }
        public Album? Album { get; set; }
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public string Url { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public ICollection<Category> Categories { get; set; } = [];
        public ICollection<Genre> Genres { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}