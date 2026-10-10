using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BabaTune.Application.DTO.Notices
{
	public class CreateServerNoticeDto
	{
		[Required]
		public string Text { get; set; } = null!;
		public string? Url { get; set; }
		public Guid? RecipientId { get; set; }
		public IFormFile? ImageFile { get; set; }
	}
}