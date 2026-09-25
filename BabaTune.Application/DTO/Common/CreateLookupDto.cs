using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.DTO.Common
{
	public class CreateLookupDto
	{
		public string Name { get; set; } = string.Empty;
		public IFormFile? ImageFile { get; set; }
	}
}
