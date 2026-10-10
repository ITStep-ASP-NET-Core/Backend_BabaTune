using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Common;
using BabaTune.Infrastructure.Data;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Repositories
{
	public class UserRepository : GenericRepository<User, Guid>, IUserRepository
	{
		public UserRepository ( ApplicationContext context ) : base(context) { }

		public async Task<User?> GetByEmailAsync ( string email )
		{
			return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
		}

		public async Task<bool> ExistsByEmailAsync ( string email )
		{
			return await _dbSet.AnyAsync(u => u.Email == email);
		}

		public async Task<PagedResult<User>> SearchByNameAsync ( string query, int pageNumber, int pageSize )
		{
			var q = _dbSet.AsNoTracking()
				.Where(u => u.Name.ToLower().Contains(query.ToLower()))
				.OrderBy(u => u.Name);

			return await q.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<PagedResult<User>> GetAllPagedAsync ( int pageNumber, int pageSize )
		{
			var q = _dbSet.AsNoTracking().OrderByDescending(u => u.CreatedAt);
			return await q.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<Room?> GetOwnedRoomAsync ( Guid userId )
		{
			return await _context.Set<Room>().AsNoTracking()
				.Include(r => r.Owner)
				.Include(r => r.CurrentSong)
					.ThenInclude(s => s!.User)
				.FirstOrDefaultAsync(r => r.OwnerId == userId);
		}

		public async Task<HashSet<Guid>> GetAllIdsAsync ( )
		{
			return await _dbSet.AsNoTracking().Select(u => u.Id).ToHashSetAsync();
		}
	}
}
