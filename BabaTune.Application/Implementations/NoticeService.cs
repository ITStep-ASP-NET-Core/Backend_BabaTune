using BabaTune.Application.Common;
using BabaTune.Application.DTO.Notices;
using BabaTune.Application.DTO.Users;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Mappers;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Interfaces;

namespace BabaTune.Application.Implementations
{
	public class NoticeService : INoticeService
	{
		private readonly IUnitOfWork _uow;

		public NoticeService ( IUnitOfWork uow )
		{
			_uow = uow;
		}

		public async Task<PagedResult<NoticeDto>> GetByRecipientAsync ( Guid recipientId, int pageNumber, int pageSize )
		{
			var notices = await _uow.Notices.GetByRecipientAsync(recipientId, pageNumber, pageSize);
			var items = new List<NoticeDto>();

			foreach (var notice in notices.Items)
			{
				UserSummaryDto? sender = null;
				if (notice.SenderId is not null)
				{
					var senderUser = await _uow.Users.GetByIdAsync(notice.SenderId.Value);
					if (senderUser is not null)
						sender = UserMapper.ToSummaryDto(senderUser);
				}

				items.Add(NoticeMapper.ToDto(notice, sender));
			}

			return new PagedResult<NoticeDto>
			{
				Items = items,
				PageNumber = notices.PageNumber,
				PageSize = notices.PageSize,
				TotalCount = notices.TotalCount
			};
		}

		public async Task<int> GetUnreadCountAsync ( Guid recipientId )
		{
			return await _uow.Notices.GetUnreadCountAsync(recipientId);
		}

		public async Task<Result> MarkAsReadAsync ( Guid noticeId, Guid recipientId )
		{
			var notice = await _uow.Notices.GetByIdAsync(noticeId);
			if (notice is null)
				return Result.Fail("Notice not found.");

			if (notice.RecipientId != recipientId)
				return Result.Fail("This notice does not belong to you.");

			await _uow.Notices.MarkAsReadAsync(noticeId);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task CreateAsync ( NoticeType type, Guid? senderId, Guid recipientId, string title, string? text, string? url = null, string? imageUrl = null )
		{
			var notice = new Notice
			{
				Id = Guid.NewGuid(),
				Type = type,
				Title = title,
				Text = text,
				IsRead = false,
				SenderId = senderId,
				RecipientId = recipientId,
				Url = url,
				ImageUrl = imageUrl,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};

			await _uow.Notices.AddAsync(notice);
			await _uow.SaveChangesAsync();
		}
	}
}
