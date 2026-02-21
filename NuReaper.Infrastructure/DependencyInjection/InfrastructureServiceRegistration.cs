using Microsoft.Extensions.DependencyInjection;
using NuReaper.Domain.Abstractions;
using NuReaper.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using NuReaper.Application.Interfaces.Scanners;
using NuReaper.Infrastructure.Repositories.Scanners;



namespace NuReaper.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration) 
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPackageRepository, PackageRepository>();
            services.AddScoped<IScanRepository, ScanRepository>();
            services.AddScoped<IAssemblyScanner, NetworkApiCallScan>();

            CorsServiceRegistration.AddCorsServices(services);
            DatabaseServiceRegistration.AddDatabaseServices(services, configuration);
            ScannerServiceRegistration.AddScannerServices(services);
            return services;
        }
    }
}
