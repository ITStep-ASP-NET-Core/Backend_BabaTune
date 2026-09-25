using BabaTune.Domain.Common;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface IGenericRepository<T, in TKey> where T : class
	{
		Task<ICollection<T>> GetAllAsync ( );
		Task<T?> GetByIdAsync ( TKey id );
		Task AddAsync ( T obj );
		void Update ( T obj );
		void Delete ( T obj );
	}
}