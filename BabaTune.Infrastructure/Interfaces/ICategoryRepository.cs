using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface ICategoryRepository : IGenericRepository<Category, int>
	{
		Task<PagedResult<Category>> SearchByNameAsync ( string query, int pageNumber, int pageSize );
	}
}
