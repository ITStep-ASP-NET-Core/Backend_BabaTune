using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface IGenreRepository : IGenericRepository<Genre, int>
	{
		Task<PagedResult<Genre>> SearchByNameAsync ( string query, int pageNumber, int pageSize );
	}
}
