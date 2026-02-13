using Microsoft.Extensions.DependencyInjection;

namespace App.Infrastructure.DependencyInjection
{
    public static class CorsServiceRegistration
    {
        public static IServiceCollection AddCorsServices(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder =>
                    {
                        builder.AllowAnyOrigin()
                               .AllowAnyMethod()
                               .AllowAnyHeader();
                    });
            });
            return services;
        }
    }
}
