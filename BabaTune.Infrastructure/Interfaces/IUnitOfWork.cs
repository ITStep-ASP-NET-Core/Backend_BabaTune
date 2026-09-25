using BabaTune.Domain.Entities;

namespace BabaTune.Infrastructure.Interfaces
{
	public interface IUnitOfWork
	{
		IUserRepository Users { get; }
		IRefreshTokenRepository RefreshTokens { get; }
		ISubscribeRepository Subscribes { get; }
		ISongRepository Songs { get; }
		IStorageRepository Storage { get; }
		IAlbumRepository Albums { get; }
		ICategoryRepository Categories { get; }
		IGenreRepository Genres { get; }
		IPlaylistRepository Playlists { get; }
		INoticeRepository Notices { get; }
		IListenHistoryRepository ListenHistories { get; }

		Task SaveChangesAsync ( );
	}
}
