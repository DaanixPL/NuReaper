using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NuReaper.Infrastructure.Repositories.GraphBuilders;
using NuReaper.Infrastructure.Repositories.GraphBuilders.Conditions;
using NuReaper.Infrastructure.Repositories.GraphBuilders.Conditions.Interfaces;
using NuReaper.Infrastructure.Repositories.GraphBuilders.Interfaces;

namespace NuReaper.Infrastructure.DependencyInjection
{
    public static class GraphBuilderServiceRegistration
    {
        public static IServiceCollection AddGraphBuilderServices(this IServiceCollection services, IConfiguration configuration) 
        {
            // Graph builder
            services.AddScoped<IBreadthFirstSearch, BreadthFirstSearch>();
            services.AddScoped<IBuildRecursiveAsync, BuildRecursiveAsync>();
            services.AddScoped<IDownloadAndExtractNuspecAsync, DownloadAndExtractNuspecAsync>();

            // Conditions
            services.AddScoped<IConditions, Conditions>();
            return services;
        }
    }
}
