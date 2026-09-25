using BabaTune.Application.Implementations;
using BabaTune.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace BabaTune.Application.ServiceProviderExtensions
{
    public static class ApplicationServicesExtensions
	{
        public static void AddApplicationServices ( this IServiceCollection services )
		{
			services.AddScoped<IPasswordHasher, Argon2PasswordHasher>();
			services.AddScoped<IAuthService, AuthService>();
			services.AddScoped<IUserService, UserService>();
			services.AddScoped<ISongService, SongService>();
			services.AddScoped<IAlbumService, AlbumService>();
			services.AddScoped<IPlaylistService, PlaylistService>();
			services.AddScoped<ICategoryService, CategoryService>();
			services.AddScoped<IGenreService, GenreService>();
			services.AddScoped<INoticeService, NoticeService>();
			services.AddScoped<ISubscribeService, SubscribeService>();
			services.AddScoped<IListenHistoryService, ListenHistoryService>();
		}
	}
}