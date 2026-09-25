using BabaTune.Domain.Entities;
using BabaTune.Application.DTO.Users;

namespace BabaTune.Application.DTO.Notices
{
	public class NoticeDto
	{
		public Guid Id { get; set; }
		public NoticeType Type { get; set; }
		public string Title { get; set; } = string.Empty;
		public string? Text { get; set; }
		public bool IsRead { get; set; }
		public string? ImageUrl { get; set; }
		public string? Url { get; set; }
		public UserSummaryDto? Sender { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
