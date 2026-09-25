using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface IUserRepository : IGenericRepository<User, Guid>
	{
		Task<User?> GetByEmailAsync ( string email );
		Task<bool> ExistsByEmailAsync ( string email );
		Task<PagedResult<User>> SearchByNameAsync ( string query, int pageNumber, int pageSize );
		Task<PagedResult<User>> GetAllPagedAsync ( int pageNumber, int pageSize );
	}
}
