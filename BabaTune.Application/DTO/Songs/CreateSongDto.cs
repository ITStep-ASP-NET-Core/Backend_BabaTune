using BabaTune.Application.Validation;
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
		[AudioFile]
		public IFormFile AudioFile { get; set; } = null!;
		[ImageFile]
		public IFormFile? ImageFile { get; set; }
	}
}
