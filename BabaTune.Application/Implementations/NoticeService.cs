using BabaTune.Application.Common;
using BabaTune.Application.DTO.Notices;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Mappers;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Interfaces;
using static BabaTune.Application.Common.Constants;

namespace BabaTune.Application.Implementations
{
	public class NoticeService : INoticeService
	{
		private readonly IUnitOfWork _uow;

		public NoticeService ( IUnitOfWork uow )
		{
			_uow = uow;
		}

		public Task<PagedResult<NoticeDto>> GetAllAsync ( Guid recipientId, int pageNumber, int pageSize )
			=> GetFeedAsync(recipientId, false, pageNumber, pageSize);

		public Task<PagedResult<NoticeDto>> GetUnreadAsync ( Guid recipientId, int pageNumber, int pageSize )
			=> GetFeedAsync(recipientId, true, pageNumber, pageSize);

		public async Task<int> GetUnreadCountAsync ( Guid recipientId )
		{
			return await _uow.Notices.GetUnreadCountAsync(recipientId);
		}

		public async Task<Result> MarkAsReadAsync ( Guid noticeId, Guid recipientId )
		{
			var notice = await _uow.Notices.GetByIdAsync(noticeId);
			if(notice is null)
				return Result.Fail("Notice not found.");

			if(notice.RecipientId != recipientId)
				return Result.Fail("This notice does not belong to you.");

			if(notice is MessageNotice message)
				await _uow.Notices.MarkChatAsReadAsync(recipientId, message.ChatId);
			else
				await _uow.Notices.MarkAsReadAsync(noticeId);

			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task MarkChatAsReadAsync ( Guid chatId, Guid recipientId )
		{
			await _uow.Notices.MarkChatAsReadAsync(recipientId, chatId);
			await _uow.SaveChangesAsync();
		}

		public async Task NotifyFriendRequestAsync ( Guid senderId, Guid recipientId, Guid friendshipId )
		{
			await AddForAsync([recipientId], id => Init(new FriendRequestNotice
			{
				SenderId = senderId,
				FriendShipId = friendshipId
			}, id));
		}

		public async Task NotifyMessageAsync ( Guid chatId, IEnumerable<Guid> recipientIds )
		{
			await AddForAsync(recipientIds.Distinct().ToList(), id => Init(new MessageNotice
			{
				ChatId = chatId
			}, id));
		}

		public async Task NotifyNewSongAsync ( Guid authorId, Guid songId )
		{
			var subscribers = await _uow.Subscribes.GetSubscriberIdsAsync(authorId);
			await AddForAsync(subscribers, id => Init(new NewSongNotice
			{
				AuthorId = authorId,
				SongId = songId
			}, id));
		}

		public async Task NotifyNewAlbumAsync ( Guid authorId, Guid albumId )
		{
			var subscribers = await _uow.Subscribes.GetSubscriberIdsAsync(authorId);
			await AddForAsync(subscribers, id => Init(new NewAlbumNotice
			{
				AuthorId = authorId,
				AlbumId = albumId
			}, id));
		}

		public async Task NotifyNewRoomAsync ( Guid ownerId, Guid roomId )
		{
			var subscribers = await _uow.Subscribes.GetSubscriberIdsAsync(ownerId);
			await AddForAsync(subscribers, id => Init(new NewRoomNotice
			{
				OwnerId = ownerId,
				RoomId = roomId
			}, id));
		}

		public async Task NotifyNewSubscriptionAsync ( Guid recipientId, Guid subscribeId )
		{
			await AddForAsync([recipientId], id => Init(new NewSubscriptionNotice
			{
				SubscribeId = subscribeId
			}, id));
		}

		public async Task<Result> CreateServerAsync ( CreateServerNoticeDto dto )
		{
			HashSet<Guid> recipients;
			if(dto.RecipientId is not null)
			{
				if(await _uow.Users.GetByIdAsync(dto.RecipientId.Value) is null)
					return Result.Fail("User not found.");

				recipients = [dto.RecipientId.Value];
			}
			else
			{
				recipients = await _uow.Users.GetAllIdsAsync();
			}

			string? imageUrl = null;
			if(dto.ImageFile is not null)
			{
				await using var imageStream = dto.ImageFile.OpenReadStream();
				imageUrl = await _uow.Storage.UploadAsync(imageStream, dto.ImageFile.FileName, dto.ImageFile.ContentType, Folders.NoticeImages);
			}

			try
			{
				await AddForAsync(recipients, id => Init(new ServerNotice
				{
					Text = dto.Text,
					Url = dto.Url,
					ImageUrl = imageUrl
				}, id));
			}
			catch
			{
				await DeleteQuietlyAsync(imageUrl);
				throw;
			}

			return Result.Ok();
		}

		public async Task<Result> UpdateServerAsync ( Guid id, UpdateServerNoticeDto dto )
		{
			if(await _uow.Notices.GetByIdAsync(id) is not ServerNotice notice)
				return Result.Fail("Server notice not found.");

			notice.Text = dto.Text;
			notice.Url = dto.Url;
			notice.UpdatedAt = DateTime.UtcNow;

			_uow.Notices.Update(notice);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> DeleteServerAsync ( Guid id )
		{
			if(await _uow.Notices.GetByIdAsync(id) is not ServerNotice notice)
				return Result.Fail("Server notice not found.");

			var imageUrl = notice.ImageUrl;

			_uow.Notices.Delete(notice);
			await _uow.SaveChangesAsync();

			if(!string.IsNullOrEmpty(imageUrl) && !await _uow.Notices.IsImageUsedAsync(imageUrl, id))
				await DeleteQuietlyAsync(imageUrl);

			return Result.Ok();
		}

		private async Task<PagedResult<NoticeDto>> GetFeedAsync ( Guid recipientId, bool unreadOnly, int pageNumber, int pageSize )
		{
			var feed = await _uow.Notices.GetFeedAsync(recipientId, unreadOnly, pageNumber, pageSize);

			if(feed.Items.Count == 0)
				return new PagedResult<NoticeDto>
				{
					Items = [],
					PageNumber = feed.PageNumber,
					PageSize = feed.PageSize,
					TotalCount = feed.TotalCount
				};

			var ids = feed.Items.Select(r => r.Id).ToList();
			var notices = (await _uow.Notices.GetDetailedByIdsAsync(ids)).ToDictionary(n => n.Id);

			var chatIds = notices.Values.OfType<MessageNotice>().Select(n => n.ChatId).Distinct().ToList();
			var titles = chatIds.Count == 0
				? new Dictionary<Guid, string>()
				: await _uow.Notices.GetChatTitlesAsync(recipientId, chatIds);

			var items = new List<NoticeDto>();
			foreach(var row in feed.Items)
			{
				if(!notices.TryGetValue(row.Id, out var notice))
					continue;

				string? title = notice is MessageNotice message ? titles.GetValueOrDefault(message.ChatId) : null;
				items.Add(NoticeMapper.ToDto(notice, row.Count, title));
			}

			return new PagedResult<NoticeDto>
			{
				Items = items,
				PageNumber = feed.PageNumber,
				PageSize = feed.PageSize,
				TotalCount = feed.TotalCount
			};
		}

		private async Task AddForAsync<T> ( ICollection<Guid> recipientIds, Func<Guid, T> factory ) where T : Notice
		{
			if(recipientIds.Count == 0)
				return;

			var notices = recipientIds.Select(factory).ToList();
			await _uow.Notices.AddRangeAsync(notices);
			await _uow.SaveChangesAsync();
		}

		private static T Init<T> ( T notice, Guid recipientId ) where T : Notice
		{
			notice.Id = Guid.NewGuid();
			notice.RecipientId = recipientId;
			notice.IsRead = false;
			notice.CreatedAt = DateTime.UtcNow;
			notice.UpdatedAt = DateTime.UtcNow;
			return notice;
		}

		private async Task DeleteQuietlyAsync ( string? url )
		{
			if(string.IsNullOrEmpty(url))
				return;

			try
			{
				await _uow.Storage.DeleteAsync(url);
			}
			catch
			{
			}
		}
	}
}