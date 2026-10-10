using BabaTune.Application.DTO.Users;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.DTO.Notices
{
	public class NoticeDto
	{
		public Guid Id { get; set; }
		public NoticeType Type { get; set; }
		public bool IsRead { get; set; }
		public int Count { get; set; } = 1;
		public UserSummaryDto? Actor { get; set; }
		public Guid? TargetId { get; set; }
		public string? Title { get; set; }
		public string? Text { get; set; }
		public string? ImageUrl { get; set; }
		public string? Url { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}