using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.DTO.Playlists
{
	public class CreatePlaylistDto
	{
		public string? Name { get; set; }
		public IFormFile? ImageFile { get; set; }
	}
}
