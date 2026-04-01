using Microsoft.Extensions.DependencyInjection;
using NuReaper.Application.Interfaces.Jobs;
using NuReaper.Infrastructure.Repositories.Jobs;

namespace NuReaper.Infrastructure.DependencyInjection
{
    public static class JobServiceRegistration
    {
        public static IServiceCollection AddJobServices(this IServiceCollection services) 
        {
            services.AddSingleton<IScanJobService, ScanJobService>();

            return services;
        }
    }
}
