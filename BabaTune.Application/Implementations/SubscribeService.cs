using BabaTune.Application.Common;
using BabaTune.Application.DTO.Users;
using BabaTune.Application.Interfaces;
using BabaTune.Application.Mappers;
using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Interfaces;

namespace BabaTune.Application.Implementations
{
	public class SubscribeService : ISubscribeService
	{
		private readonly IUnitOfWork _uow;

		public SubscribeService ( IUnitOfWork uow )
		{
			_uow = uow;
		}

		public async Task<PagedResult<UserSummaryDto>> GetSubscriptionsAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var subscriptions = await _uow.Subscribes.GetSubscriptionsAsync(userId, pageNumber, pageSize);
			return await ToDtoPageAsync(subscriptions, s => s.SubscribedTo);
		}

		public async Task<PagedResult<UserSummaryDto>> GetSubscribersAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var subscribers = await _uow.Subscribes.GetSubscribersAsync(userId, pageNumber, pageSize);
			return await ToDtoPageAsync(subscribers, s => s.Subscriber);
		}

		public async Task<Result> SubscribeAsync ( Guid subscriberId, Guid subscribedToId )
		{
			if (subscriberId == subscribedToId)
				return Result.Fail("You cannot subscribe to yourself.");

			if (await _uow.Subscribes.ExistsAsync(subscriberId, subscribedToId))
				return Result.Fail("Already subscribed.");

			var subscribe = new Subscribe
			{
				Id = Guid.NewGuid(),
				SubscriberId = subscriberId,
				SubscribedToId = subscribedToId,
				SubscribedAt = DateTime.UtcNow
			};

			await _uow.Subscribes.AddAsync(subscribe);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		public async Task<Result> UnsubscribeAsync ( Guid subscriberId, Guid subscribedToId )
		{
			var subscribe = await _uow.Subscribes.GetAsync(subscriberId, subscribedToId);
			if (subscribe is null)
				return Result.Fail("Subscription not found.");

			_uow.Subscribes.Delete(subscribe);
			await _uow.SaveChangesAsync();

			return Result.Ok();
		}

		private async Task<PagedResult<UserSummaryDto>> ToDtoPageAsync ( PagedResult<Subscribe> source, Func<Subscribe, User> selectUser )
		{
			var items = new List<UserSummaryDto>();

			foreach (var subscribe in source.Items)
			{
				var user = selectUser(subscribe);
				var listenersCount = await _uow.Subscribes.GetSubscribersCountAsync(user.Id);
				items.Add(UserMapper.ToSummaryDto(user));
			}

			return new PagedResult<UserSummaryDto>
			{
				Items = items,
				PageNumber = source.PageNumber,
				PageSize = source.PageSize,
				TotalCount = source.TotalCount
			};
		}
	}
}
