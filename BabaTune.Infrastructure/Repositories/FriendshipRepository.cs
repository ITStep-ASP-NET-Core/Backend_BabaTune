using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Common;
using BabaTune.Infrastructure.Data;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Repositories
{
	public class FriendshipRepository : GenericRepository<Friendship, Guid>, IFriendshipRepository
	{
		public FriendshipRepository ( ApplicationContext context ) : base(context) { }

		public async Task<Friendship?> GetByUsersAsync ( Guid userId, Guid otherUserId )
		{
			return await _dbSet.FirstOrDefaultAsync(f =>
				(f.SenderId == userId && f.RecipientId == otherUserId) ||
				(f.SenderId == otherUserId && f.RecipientId == userId));
		}

		public async Task<bool> ExistsAsync ( Guid userId, Guid otherUserId )
		{
			return await _dbSet.AnyAsync(f =>
				(f.SenderId == userId && f.RecipientId == otherUserId) ||
				(f.SenderId == otherUserId && f.RecipientId == userId));
		}

		public async Task<PagedResult<Friendship>> GetByStatusWithUsersAsync ( Guid userId, FriendshipStatus status, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking()
				.Where(f => (f.SenderId == userId || f.RecipientId == userId) && f.Status == status)
				.Include(f => f.Sender)
				.Include(f => f.Recipient)
				.OrderByDescending(f => f.UpdatedAt)
				.ThenBy(f => f.Id);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<PagedResult<Friendship>> GetIncomingWithUsersAsync ( Guid recipientId, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking()
				.Where(f => f.RecipientId == recipientId && f.Status == FriendshipStatus.Pending)
				.Include(f => f.Sender)
				.Include(f => f.Recipient)
				.OrderByDescending(f => f.CreatedAt)
				.ThenBy(f => f.Id);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<PagedResult<Friendship>> GetBlockedWithUsersAsync ( Guid blockerId, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking()
				.Where(f => f.SenderId == blockerId && f.Status == FriendshipStatus.Blocked)
				.Include(f => f.Sender)
				.Include(f => f.Recipient)
				.OrderByDescending(f => f.UpdatedAt)
				.ThenBy(f => f.Id);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}
	}
}
