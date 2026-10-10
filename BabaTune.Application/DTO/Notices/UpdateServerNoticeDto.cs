using System.ComponentModel.DataAnnotations;

namespace BabaTune.Application.DTO.Notices
{
	public class UpdateServerNoticeDto
	{
		[Required]
		public string Text { get; set; } = null!;
		public string? Url { get; set; }
	}
}