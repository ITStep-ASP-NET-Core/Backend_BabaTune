using BabaTune.Application.DTO.Friendships;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.Mappers
{
	public static class FriendshipMapper
	{
		public static FriendshipDto ToDto ( Friendship friendship, Guid currentUserId )
		{
			var other = friendship.SenderId == currentUserId ? friendship.Recipient : friendship.Sender;

			return new FriendshipDto
			{
				Id = friendship.Id,
				User = UserMapper.ToSummaryDto(other!),
				ChatId = friendship.ChatId,
				Status = friendship.Status,
				CreatedAt = friendship.CreatedAt
			};
		}
	}
}
