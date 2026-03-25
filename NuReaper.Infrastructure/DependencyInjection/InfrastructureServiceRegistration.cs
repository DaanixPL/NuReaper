using Microsoft.Extensions.DependencyInjection;
using NuReaper.Domain.Abstractions;
using NuReaper.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using NuReaper.Application.Interfaces.Scanners;
using NuReaper.Application.Interfaces.Dependencies;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis;



namespace NuReaper.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration) 
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPackageRepository, PackageRepository>();
            services.AddScoped<IScanRepository, ScanRepository>();
            services.AddScoped<IAssemblyScanner, AssemblyScanner>();
            services.AddScoped<IDependencyRepository, DependencyRepository>();
            services.AddScoped<INetworkApiCallScan, NetworkApiCallScan>();

            CorsServiceRegistration.AddCorsServices(services);
            DatabaseServiceRegistration.AddDatabaseServices(services, configuration);
            ScannerServiceRegistration.AddScannerServices(services);
            ParsersServiceRegistration.AddParsersServices(services);
            GraphBuilderServiceRegistration.AddGraphBuilderServices(services, configuration);
            FileHelpersServiceRegistration.AddFileHelpersServices(services);
            return services;
        }
    }
}
