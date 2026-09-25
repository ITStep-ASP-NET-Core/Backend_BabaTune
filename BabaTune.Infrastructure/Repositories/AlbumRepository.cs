using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Common;
using BabaTune.Infrastructure.Data;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Repositories
{
	public class AlbumRepository : GenericRepository<Album, Guid>, IAlbumRepository
	{
		public AlbumRepository ( ApplicationContext context ) : base(context) { }

		public async Task<PagedResult<Album>> GetByAuthorAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking()
				.Where(a => a.UserId == userId)
				.Include(a => a.User)
				.OrderByDescending(a => a.CreatedAt);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<PagedResult<Album>> SearchAsync ( string query, int pageNumber, int pageSize )
		{
			var q = _dbSet.AsNoTracking()
				.Where(a => a.Name.ToLower().Contains(query.ToLower()))
				.Include(a => a.User)
				.OrderByDescending(a => a.CreatedAt);

			return await q.ToPagedResultAsync(pageNumber, pageSize);
		}
	}
}
