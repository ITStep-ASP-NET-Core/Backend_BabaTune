using BabaTune.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BabaTune.Application.ServiceProviderExtensions
{
	public static class StorageServiceExtensions
	{
		public static IServiceCollection AddStorageService ( this IServiceCollection services, IConfigurationSection configuration )
		{
			services.Configure<FirebaseStorageOptions>(configuration);

			return services;
		}
	}
}
