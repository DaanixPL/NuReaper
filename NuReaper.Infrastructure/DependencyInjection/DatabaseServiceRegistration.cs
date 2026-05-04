using NuReaper.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace NuReaper.Infrastructure.DependencyInjection
{
    public static class DatabaseServiceRegistration
    {
        public static IServiceCollection AddDatabaseServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlite("Data Source=nureaper_dev.db"), ServiceLifetime.Scoped);
            // services.AddDbContext<AppDbContext>(options =>
            // options.UseMySql(configuration.GetConnectionString("DefaultConnection"),
            //     new MySqlServerVersion(new Version(8, 0, 29))));

            return services;
        }
    }
}
