using BabaTune.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Common
{
	public static class QueryableExtensions
	{
		public static async Task<PagedResult<T>> ToPagedResultAsync<T> ( this IQueryable<T> query, int pageNumber, int pageSize )
		{
			pageNumber = pageNumber < 1 ? 1 : pageNumber;
			pageSize = pageSize < 1 ? 1 : pageSize;

			var totalCount = await query.CountAsync();
			var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync();

			return new PagedResult<T>
			{
				Items = items,
				TotalCount = totalCount,
				PageNumber = pageNumber,
				PageSize = pageSize
			};
		}
	}
}
