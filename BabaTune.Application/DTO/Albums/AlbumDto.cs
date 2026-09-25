
namespace BabaTune.Application.DTO.Albums
{
	public class AlbumDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }
		public string? ImageUrl { get; set; }
		public Guid? AuthorId { get; set; }
		public string? AuthorName { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
