using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.DTO.Songs
{
	public class CreateSongDto
	{
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }
		public Guid? AlbumId { get; set; }
		public List<int> CategoryIds { get; set; } = [];
		public List<int> GenreIds { get; set; } = [];
		public IFormFile AudioFile { get; set; } = null!;
		public IFormFile? ImageFile { get; set; }
	}
}
