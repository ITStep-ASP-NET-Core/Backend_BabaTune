using BabaTune.Infrastructure.Data;
using BabaTune.Infrastructure.Interfaces;

namespace BabaTune.Infrastructure.Repositories
{
	public class UnitOfWork : IUnitOfWork
	{
		private readonly ApplicationContext _context;

		public IUserRepository Users { get; }
		public IRefreshTokenRepository RefreshTokens { get; }
		public ISubscribeRepository Subscribes { get; }
		public ISongRepository Songs { get; }
		public IStorageRepository Storage { get; }
		public IAlbumRepository Albums { get; }
		public ICategoryRepository Categories { get; }
		public IGenreRepository Genres { get; }
		public IPlaylistRepository Playlists { get; }
		public INoticeRepository Notices { get; }
		public IListenHistoryRepository ListenHistories { get; }

		public UnitOfWork ( 
			ApplicationContext context,
			IUserRepository users,
			IRefreshTokenRepository refreshTokens,
			ISubscribeRepository subscribes,
			ISongRepository songs,
			IStorageRepository storage,
			IAlbumRepository albums,
			IPlaylistRepository playlists,
			INoticeRepository notices,
			IListenHistoryRepository listenHistories,
			ICategoryRepository categories,
			IGenreRepository genres
		)
		{
			_context = context;
			Users = users;
			RefreshTokens = refreshTokens;
			Subscribes = subscribes;
			Songs = songs;
			Storage = storage;
			Albums = albums;
			Playlists = playlists;
			Notices = notices;
			ListenHistories = listenHistories;
			Categories = categories;
			Genres = genres;
		}

		public async Task SaveChangesAsync ( )
		{
			await _context.SaveChangesAsync();
		}
	}
}
