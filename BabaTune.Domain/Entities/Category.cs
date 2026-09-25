
namespace BabaTune.Domain.Entities
{ 
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public ICollection<Song> Songs { get; set; } = [];
    }
}