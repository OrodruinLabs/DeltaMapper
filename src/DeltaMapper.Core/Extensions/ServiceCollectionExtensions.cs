using DeltaMapper.Configuration;
using DeltaMapper.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace DeltaMapper;

/// <summary>
/// Extension methods for registering DeltaMapper in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers DeltaMapper services: MapperConfiguration (singleton) and IMapper (singleton).
        /// </summary>
        public IServiceCollection AddDeltaMapper(Action<MapperConfigurationBuilder> configure)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configure);
            var builder = new MapperConfigurationBuilder();
            configure(builder);
            var config = builder.Build();
            services.AddSingleton(config);
            services.AddSingleton<IMapper>(sp => new Mapper(sp.GetRequiredService<MapperConfiguration>()));
            return services;
        }
    }
}
