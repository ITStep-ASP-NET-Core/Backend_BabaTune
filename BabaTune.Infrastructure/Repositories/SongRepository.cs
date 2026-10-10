using BabaTune.Domain.Common;
using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Common;
using BabaTune.Infrastructure.Data;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Repositories
{
	public class SongRepository : GenericRepository<Song, Guid>, ISongRepository
	{
		public SongRepository ( ApplicationContext context ) : base(context) { }

		public async Task<ICollection<Song>> GetByIdsAsync ( ICollection<Guid> ids )
		{
			return await _dbSet.AsNoTracking()
				.Include(s => s.User)
				.Where(s => ids.Contains(s.Id))
				.ToListAsync();
		}

		public async Task<Song?> GetWithAllAsync ( Guid id )
		{
			return await _dbSet
				.Include(s => s.User)
				.Include(s => s.Categories)
				.Include(s => s.Genres)
				.Include(s => s.Album)
				.FirstOrDefaultAsync(s => s.Id == id);
		}

		public async Task<PagedResult<Song>> GetByAuthorAsync ( Guid userId, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking()
				.Where(s => s.UserId == userId)
				.Include(s => s.User)
				.OrderByDescending(s => s.CreatedAt);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<int> GetCountByAuthorAsync ( Guid userId )
		{
			return await _dbSet.CountAsync(s => s.UserId == userId);
		}

		public async Task<PagedResult<Song>> GetByAlbumAsync ( Guid albumId, int pageNumber, int pageSize )
		{
			var query = _dbSet.AsNoTracking()
				.Where(s => s.AlbumId == albumId)
				.Include(s => s.User)
				.OrderBy(s => s.CreatedAt);

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<PagedResult<Song>> GetByFiltersAsync (
			string? searchQuery,
			Guid? authorId,
			Guid? albumId,
			ICollection<int>? categoryIds,
			ICollection<int>? genreIds,
			int pageNumber,
			int pageSize )
		{
			var query = _dbSet.AsQueryable();

			if (!string.IsNullOrWhiteSpace(searchQuery))
				query = query.Where(s => s.Name.ToLower().Contains(searchQuery.ToLower()));

			if (authorId is not null)
				query = query.Where(s => s.UserId == authorId);

			if (albumId is not null)
				query = query.Where(s => s.AlbumId == albumId);

			if (categoryIds != null && categoryIds.Count > 0)
				query = query.Where(s => s.Categories.Any(c => categoryIds.Contains(c.Id)));

			if (genreIds != null && genreIds.Count > 0)
				query = query.Where(s => s.Genres.Any(g => genreIds.Contains(g.Id)));

			query = query
				.Include(s => s.User)
				.OrderByDescending(s => s.CreatedAt)
				.AsNoTracking();

			return await query.ToPagedResultAsync(pageNumber, pageSize);
		}

		public async Task<HashSet<Guid>> GetIdsByAlbumAsync ( Guid albumId )
		{
			return await _context.Songs
				.Where(i => i.AlbumId == albumId)
				.OrderBy(i => i.CreatedAt)
				.Select(i => i.Id)
				.ToHashSetAsync();
		}

		public async Task<int> GetCountByAlbumAsync ( Guid albumId )
		{
			return await _context.Songs.CountAsync(s => s.AlbumId == albumId);
		}

		public async Task<(List<Song> Items, int TotalCount)> GetWithoutIdsAsync ( ICollection<Guid> ids, int skip, int take )
		{
			var query = _dbSet.AsNoTracking().Where(s => !ids.Contains(s.Id));

			var total = await query.CountAsync();
			if(take <= 0)
				return ([], total);

			var items = await query
				.Include(s => s.User)
				.OrderByDescending(s => s.CreatedAt)
				.ThenBy(s => s.Id)
				.Skip(skip)
				.Take(take)
				.ToListAsync();

			return (items, total);
		}
	}
}
