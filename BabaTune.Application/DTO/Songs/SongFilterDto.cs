namespace BabaTune.Application.DTO.Songs
{
	public class SongFilterDto
	{
		public string? SearchQuery { get; set; }

		public Guid? AuthorId { get; set; }

		public Guid? AlbumId { get; set; }

		public ICollection<int>? CategoryIds { get; set; }

		public ICollection<int>? GenreIds { get; set; }
	}
}
