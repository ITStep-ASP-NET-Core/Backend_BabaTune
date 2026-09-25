
namespace BabaTune.Domain.Entities
{
    public class Album : IAuditable
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public ICollection<Song> Songs { get; set; } = [];
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
