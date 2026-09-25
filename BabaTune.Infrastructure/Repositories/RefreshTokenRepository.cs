using BabaTune.Domain.Entities;
using BabaTune.Infrastructure.Data;
using BabaTune.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BabaTune.Infrastructure.Repositories
{
	public class RefreshTokenRepository : GenericRepository<RefreshToken, Guid>, IRefreshTokenRepository
	{
		public RefreshTokenRepository ( ApplicationContext context ) : base(context) { }

		public async Task<RefreshToken?> GetByTokenAsync ( string token )
		{
			return await _dbSet.FirstOrDefaultAsync(t => t.Token == token);
		}

		public async Task RevokeAsync ( Guid tokenId )
		{
			var token = await _dbSet.FirstOrDefaultAsync(t => t.Id == tokenId);
			if (token is null)
				return;

			token.RevokedAt = DateTime.UtcNow;
		}

		public async Task RevokeAllForUserAsync ( Guid userId )
		{
			var tokens = await _dbSet.Where(t => t.UserId == userId && t.RevokedAt == null).ToListAsync();
			foreach (var token in tokens)
				token.RevokedAt = DateTime.UtcNow;
		}

		public async Task RemoveExpiredAsync ( )
		{
			var now = DateTime.UtcNow;
			var expired = await _dbSet.Where(t => t.ExpiresAt < now).ToListAsync();
			_dbSet.RemoveRange(expired);
		}
	}
}
