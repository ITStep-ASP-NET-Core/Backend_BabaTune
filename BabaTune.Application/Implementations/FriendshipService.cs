using BabaTune.Application.Common;
using BabaTune.Application.DTO.Friendships;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Mappers;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Interfaces;

namespace BabaTune.Application.Implementations
{
	public class FriendshipService : IFriendshipService
	{
		private readonly IUnitOfWork _uow;

		public FriendshipService ( IUnitOfWork uow )
		{
			_uow = uow;
		}

		public async Task<PagedResult<FriendshipDto>> GetFriendsAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var page = await _uow.Friendships.GetByStatusWithUsersAsync(userId, FriendshipStatus.Accepted, pageNumber, pageSize);
			return ToDtoPage(page, userId);
		}

		public async Task<PagedResult<FriendshipDto>> GetIncomingAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var page = await _uow.Friendships.GetIncomingWithUsersAsync(userId, pageNumber, pageSize);
			return ToDtoPage(page, userId);
		}

		public async Task<PagedResult<FriendshipDto>> GetBlockedAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var page = await _uow.Friendships.GetBlockedWithUsersAsync(userId, pageNumber, pageSize);
			return ToDtoPage(page, userId);
		}

		public async Task<Result> SendRequestAsync ( Guid senderId, Guid recipientId )
		{
			if (senderId == recipientId)
				return Result.Fail("You cannot send a friend request to yourself.");

			var recipient = await _uow.Users.GetByIdAsync(recipientId);
			if (recipient is null)
				return Result.Fail("User not found.");

			var existing = await _uow.Friendships.GetByUsersAsync(senderId, recipientId);
			if (existing is not null)
			{
				return existing.Status switch
				{
					FriendshipStatus.Accepted => Result.Fail("Already friends."),
					FriendshipStatus.Pending => Result.Fail("Friend request already exists."),
					_ => existing.SenderId == senderId
						? Result.Fail("Unblock this user first.")
						: Result.Fail("User not found.")
				};
			}

			var now = DateTime.UtcNow;

			var friendship = new Friendship
			{
				Id = Guid.NewGuid(),
				SenderId = senderId,
				RecipientId = recipientId,
				Status = FriendshipStatus.Pending,
				CreatedAt = now,
				UpdatedAt = now
			};

			await _uow.Friendships.AddAsync(friendship);

			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> AcceptAsync ( Guid friendshipId, Guid userId )
		{
			var friendship = await _uow.Friendships.GetByIdAsync(friendshipId);
			if (friendship is null || friendship.RecipientId != userId || friendship.Status != FriendshipStatus.Pending)
				return Result.Fail("Friend request not found.");

			var now = DateTime.UtcNow;

			var chat = new Chat
			{
				Id = Guid.NewGuid(),
				Type = ChatType.Personal,
				CreatedAt = now,
				UpdatedAt = now
			};

			await _uow.Chats.AddAsync(chat);

			friendship.ChatId = chat.Id;
			friendship.Status = FriendshipStatus.Accepted;
			friendship.UpdatedAt = now;

			_uow.Friendships.Update(friendship);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> BlockAsync ( Guid userId, Guid targetId )
		{
			if (userId == targetId)
				return Result.Fail("You cannot block yourself.");

			var target = await _uow.Users.GetByIdAsync(targetId);
			if (target is null)
				return Result.Fail("User not found.");

			var now = DateTime.UtcNow;
			var friendship = await _uow.Friendships.GetByUsersAsync(userId, targetId);

			if (friendship is null)
			{
				await _uow.Friendships.AddAsync(new Friendship
				{
					Id = Guid.NewGuid(),
					SenderId = userId,
					RecipientId = targetId,
					Status = FriendshipStatus.Blocked,
					CreatedAt = now,
					UpdatedAt = now
				});
			}
			else
			{
				if (friendship.Status == FriendshipStatus.Blocked)
				{
					return friendship.SenderId == userId
						? Result.Fail("Already blocked.")
						: Result.Fail("User not found.");
				}

				await DeleteChatAsync(friendship.ChatId);

				friendship.ChatId = null;
				friendship.SenderId = userId;
				friendship.RecipientId = targetId;
				friendship.Status = FriendshipStatus.Blocked;
				friendship.UpdatedAt = now;

				_uow.Friendships.Update(friendship);
			}

			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> RemoveAsync ( Guid friendshipId, Guid userId )
		{
			var friendship = await _uow.Friendships.GetByIdAsync(friendshipId);
			if (friendship is null || !IsRemovableBy(friendship, userId))
				return Result.Fail("Friendship not found.");

			await DeleteChatAsync(friendship.ChatId);

			_uow.Friendships.Delete(friendship);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		private static bool IsRemovableBy ( Friendship friendship, Guid userId )
			=> friendship.SenderId == userId
				|| (friendship.RecipientId == userId && friendship.Status != FriendshipStatus.Blocked);

		private async Task DeleteChatAsync ( Guid? chatId )
		{
			if (chatId is null)
				return;

			var chat = await _uow.Chats.GetByIdAsync(chatId.Value);
			if (chat is not null)
				_uow.Chats.Delete(chat);
		}

		private static PagedResult<FriendshipDto> ToDtoPage ( PagedResult<Friendship> page, Guid userId )
		{
			return new PagedResult<FriendshipDto>
			{
				Items = page.Items.Select(f => FriendshipMapper.ToDto(f, userId)).ToList(),
				PageNumber = page.PageNumber,
				PageSize = page.PageSize,
				TotalCount = page.TotalCount
			};
		}
	}
}
