using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.DTO.Albums
{
	public class CreateAlbumDto
	{
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }
		public IFormFile? ImageFile { get; set; }
	}
}
