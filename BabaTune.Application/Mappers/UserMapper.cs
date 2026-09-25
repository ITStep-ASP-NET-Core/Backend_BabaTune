using BabaTune.Application.DTO.Users;
using BabaTune.Domain.Entities;

namespace BabaTune.Application.Mappers
{
	public class UserMapper
	{
		public static UserDto ToDto ( User user, int subscriptionsCount, int tracksCount, int listenersCount ) => new()
		{
			Id = user.Id,
			Name = user.Name,
			AvatarUrl = user.AvatarUrl,
			SubscriptionsCount = subscriptionsCount,
			TracksCount = tracksCount,
			ListenersCount = listenersCount,
			IsChecked = user.IsChecked,
		};

		public static UserSummaryDto ToSummaryDto ( User user ) => new()
		{
			Id = user.Id,
			Name = user.Name,
			AvatarUrl = user.AvatarUrl,
		};
	}
}
