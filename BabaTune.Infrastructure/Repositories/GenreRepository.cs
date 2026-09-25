using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Common;
using BabaTune.Infrastructure.Data;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Repositories
{
	public class GenreRepository : GenericRepository<Genre, int>, IGenreRepository
	{
		public GenreRepository ( ApplicationContext context ) : base(context) { }

		public async Task<PagedResult<Genre>> SearchByNameAsync ( string query, int pageNumber, int pageSize )
		{
			var q = _dbSet.AsNoTracking()
				.Where(u => u.Name.ToLower().Contains(query.ToLower()))
				.OrderBy(u => u.Name);

			return await q.ToPagedResultAsync(pageNumber, pageSize);
		}
	}
}
