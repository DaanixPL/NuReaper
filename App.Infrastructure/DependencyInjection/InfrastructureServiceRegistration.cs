using Microsoft.Extensions.DependencyInjection;
using NuReaper.Domain.Abstractions;
using NuReaper.Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;



namespace App.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration) 
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPackageRepository, PackageRepository>();
            services.AddScoped<IScanRepository, ScanRepository>();

            CorsServiceRegistration.AddCorsServices(services);
            DatabaseServiceRegistration.AddDatabaseServices(services, configuration);
            return services;
        }
    }
}
