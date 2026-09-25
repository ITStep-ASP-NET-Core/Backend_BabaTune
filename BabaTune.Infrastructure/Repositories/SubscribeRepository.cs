using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Common;
using BabaTune.Infrastructure.Data;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Repositories
{
	public class SubscribeRepository : GenericRepository<Subscribe, Guid>, ISubscribeRepository
	{
		public SubscribeRepository ( ApplicationContext context ) : base(context) { }

		public async Task<Subscribe?> GetAsync ( Guid subscriberId, Guid subscribedToId )
		{
			return await _dbSet.FirstOrDefaultAsync(s =>
				s.SubscriberId == subscriberId && s.SubscribedToId == subscribedToId);
		}

		public async Task<bool> ExistsAsync ( Guid subscriberId, Guid subscribedToId )
		{
			return await _dbSet.AnyAsync(s =>
				s.SubscriberId == subscriberId && s.SubscribedToId == subscribedToId);
		}

		public async Task<PagedResult<Subscribe>> GetSubscriptionsAsync ( Guid subscriberId, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking()
				.Where(s => s.SubscriberId == subscriberId)
				.Include(s => s.SubscribedTo)
				.OrderByDescending(s => s.SubscribedAt);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<int> GetSubscriptionsCountAsync ( Guid subscriberId )
		{
			return await _dbSet.CountAsync(s => s.SubscriberId == subscriberId);
		}

		public async Task<PagedResult<Subscribe>> GetSubscribersAsync ( Guid subscribedToId, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking()
				.Where(s => s.SubscribedToId == subscribedToId)
				.Include(s => s.Subscriber)
				.OrderByDescending(s => s.SubscribedAt);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<int> GetSubscribersCountAsync ( Guid subscribedToId )
		{
			return await _dbSet.CountAsync(s => s.SubscribedToId == subscribedToId);
		}
	}
}
