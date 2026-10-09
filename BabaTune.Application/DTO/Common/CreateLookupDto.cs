using BabaTune.Application.Validation;
using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.DTO.Common
{
	public class CreateLookupDto
	{
		public string Name { get; set; } = string.Empty;
		[ImageFile]
		public IFormFile? ImageFile { get; set; }
	}
}
