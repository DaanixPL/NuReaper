using Microsoft.Extensions.DependencyInjection;
using NuReaper.Infrastructure.Repositories.FileHelpers;
using NuReaper.Infrastructure.Repositories.FileHelpers.interfaces;
using NuReaper.Infrastructure.Repositories.FileHelpers.Interfaces;

namespace NuReaper.Infrastructure.DependencyInjection
{
    public static class FileHelpersServiceRegistration
    {
        public static IServiceCollection AddFileHelpersServices(this IServiceCollection services)
        {
            services.AddScoped<IDownloadPackageAsync, DownloadPackageAsync>();
            services.AddScoped<IExtractPackage, ExtractPackage>();
            services.AddScoped<IExtractPackageInfo, ExtractPackageInfo>();
            services.AddScoped<ICalculateSha256, CalculateSha256>();
            services.AddScoped<IExtractNupkgAsync, ExtractNupkgAsync>();

            return services;
        }
    }
}
