using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface IRefreshTokenRepository : IGenericRepository<RefreshToken, Guid>
	{
		Task<RefreshToken?> GetByTokenAsync ( string token );
		Task RevokeAsync ( Guid tokenId );
		Task RevokeAllForUserAsync ( Guid userId );
		Task RemoveExpiredAsync ( );
	}
}
