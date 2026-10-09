using BabaTune.Application.DTO.Users;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.DTO.Friendships
{
	public class FriendshipDto
	{
		public Guid Id { get; set; }
		public UserSummaryDto User { get; set; } = null!;
		public Guid? ChatId { get; set; }
		public FriendshipStatus Status { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
