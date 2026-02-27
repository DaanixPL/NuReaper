using Microsoft.Extensions.DependencyInjection;
using NuReaper.Application.Interfaces.Parsers;
using NuReaper.Infrastructure.Repositories.Parsers;
using NuReaper.Infrastructure.Repositories.Parsers.Strategies;
using NuReaper.Infrastructure.Repositories.Parsers.Strategies.Interfaces;
using NuReaper.Infrastructure.Repositories.Parsers.Utils;
using NuReaper.Infrastructure.Repositories.Parsers.Utils.Interfaces;

namespace NuReaper.Infrastructure.DependencyInjection
{
    public static class ParsersServiceRegistration
    {
        public static IServiceCollection AddParsersServices(this IServiceCollection services)
        {
            // Parser
            services.AddScoped<INuspecParser, NuspecParser>();

            // Strategies
            services.AddScoped<INuGetDependencyParser, NuGetDependencyParser>();
            services.AddScoped<IFrameworkAssemblyParser, FrameworkAssemblyParser>();

            // Utils
            services.AddScoped<IParseVersionRange, ParseVersionRange>();
            services.AddScoped<IParseDependencyElement, ParseDependencyElement>();

            return services;
        }
    }
}