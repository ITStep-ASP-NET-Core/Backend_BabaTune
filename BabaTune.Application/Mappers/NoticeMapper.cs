using BabaTune.Application.DTO.Notices;
using BabaTune.Application.DTO.Users;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.Mappers
{
	public class NoticeMapper
	{
		public static NoticeDto ToDto ( Notice notice, UserSummaryDto? sender ) => new()
		{
			Id = notice.Id,
			Type = notice.Type,
			Title = notice.Title,
			Text = notice.Text,
			IsRead = notice.IsRead,
			ImageUrl = notice.ImageUrl,
			Url = notice.Url,
			Sender = sender,
			CreatedAt = notice.CreatedAt,
		};
	}
}
