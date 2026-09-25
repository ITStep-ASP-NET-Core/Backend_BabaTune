namespace BabaTune.Application.DTO.Albums
{
	public class AlbumCardDto
	{
		public Guid Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? ImageUrl { get; set; }
		public string? AuthorName { get; set; }
		public Guid? AuthorId { get; set; }
	}
}
