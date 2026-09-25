using BabaTune.Infrastructure.Data;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Repositories
{
	public class GenericRepository<T, TKey> : IGenericRepository<T, TKey> where T : class
	{
		protected readonly ApplicationContext _context;
		protected readonly DbSet<T> _dbSet;

		public GenericRepository ( ApplicationContext context )
		{
			_context = context;
			_dbSet = _context.Set<T>();
		}

		public async Task<ICollection<T>> GetAllAsync ( )
		{
			return await _dbSet.AsNoTracking().ToListAsync();
		}

		public async Task<T?> GetByIdAsync ( TKey id )
		{
			return await _dbSet.FindAsync(id);
		}

		public async Task AddAsync ( T obj )
		{
			await _dbSet.AddAsync(obj);
		}

		public void Update ( T obj )
		{
			_dbSet.Update(obj);
		}

		public void Delete ( T obj )
		{
			_dbSet.Remove(obj);
		}
	}
}