using BabaTune.Infrastructure.Interfaces;
using BabaTune.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace BabaTune.Application.ServiceProviderExtensions
{
    public static class UnitOfWorkExtensions
    {
        public static void AddUnitOfWork(this IServiceCollection services)
        {
			services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

			services.AddScoped<IUserRepository, UserRepository>();
			services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
			services.AddScoped<ISubscribeRepository, SubscribeRepository>();
			services.AddScoped<ISongRepository, SongRepository>();
			services.AddScoped<IStorageRepository, FirebaseStorageRepository>();
			services.AddScoped<IAlbumRepository, AlbumRepository>();
			services.AddScoped<IPlaylistRepository, PlaylistRepository>();
			services.AddScoped<INoticeRepository, NoticeRepository>();
			services.AddScoped<IListenHistoryRepository, ListenHistoryRepository>();
			services.AddScoped<ICategoryRepository, CategoryRepository>();
			services.AddScoped<IGenreRepository, GenreRepository>();
			services.AddScoped<IStorageRepository, FirebaseStorageRepository>();
			services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

			services.AddScoped<IUnitOfWork, UnitOfWork>();
		}
    }
}