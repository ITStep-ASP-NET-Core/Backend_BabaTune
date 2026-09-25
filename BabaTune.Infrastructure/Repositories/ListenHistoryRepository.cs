using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Common;
using BabaTune.Infrastructure.Data;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Repositories
{
	public class ListenHistoryRepository : GenericRepository<ListenHistory, Guid>, IListenHistoryRepository
	{
		public ListenHistoryRepository ( ApplicationContext context ) : base(context) { }

		public async Task<PagedResult<ListenHistory>> GetByUserAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking()
				.Where(l => l.UserId == userId)
				.Include(l => l.Song)
				.OrderByDescending(l => l.ListenedAt);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<PagedResult<Guid>> GetTopSongIdsAsync ( DateTime from, int pageNumber, int pageSize )
		{
			var scores = _dbSet.AsNoTracking()
				.Where(l => l.ListenedAt >= from)
				.GroupBy(l => l.SongId)
				.Select(g => new
				{
					SongId = g.Key,
					Score = (0.7 * g.Average(x => x.PlayedPercent) / 100.0
						+ 0.3 * ((double)g.Count(x => x.IsLiked) / g.Count()))
						* Math.Log10((double)g.Count() + 1)
				});

			var total = await scores.CountAsync();

			var ids = await scores
				.OrderByDescending(x => x.Score)
				.ThenBy(x => x.SongId)
				.Skip((pageNumber - 1) * pageSize)
				.Take(pageSize)
				.Select(x => x.SongId)
				.ToListAsync();

			return new PagedResult<Guid> {
				Items = ids,
				TotalCount = total,
				PageNumber = pageNumber,
				PageSize = pageSize
			};
		}
		public async Task<ListenHistory?> GetEntryAsync ( Guid userId, Guid songId )
		{
			return await _dbSet
				.Where(l => l.UserId == userId && l.SongId == songId)
				.OrderByDescending(l => l.ListenedAt)
				.FirstOrDefaultAsync();
		}
	}
}
