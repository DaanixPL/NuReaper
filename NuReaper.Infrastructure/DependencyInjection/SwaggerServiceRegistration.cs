using Microsoft.Extensions.DependencyInjection;

namespace NuReaper.Infrastructure.DependencyInjection
{
    public static class SwaggerServiceRegistration
    {
        public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "NuReaper API", Version = "v1" });
            });

            return services;
        }
    }
}
