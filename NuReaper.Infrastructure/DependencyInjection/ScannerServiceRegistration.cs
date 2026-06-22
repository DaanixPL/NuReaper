using Microsoft.Extensions.DependencyInjection;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Files;

namespace NuReaper.Infrastructure.DependencyInjection
{
    public static class ScannerServiceRegistration
    {
        public static IServiceCollection AddScannerServices(this IServiceCollection services)
        {
            services.AddScoped<IPatternDetector, Pattern1_NetworkToExecution>();
            services.AddScoped<IPatternDetector, Pattern2>();

            services.AddScoped<IGetAssemblyFiles, GetAssemblyFiles>();

            return services;
        }
    }
}
