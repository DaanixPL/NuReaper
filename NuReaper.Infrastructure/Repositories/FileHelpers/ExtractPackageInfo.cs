using NuReaper.Infrastructure.Repositories.FileHelpers.interfaces;

namespace NuReaper.Infrastructure.Repositories.FileHelpers
{
    public class ExtractPackageInfo : IExtractPackageInfo
    {
        /// <summary>
        /// Extracts package name and version from NuGet URL
        /// URL format: https://www.nuget.org/packages/{packageName}/{version}
        /// </summary>
        public (string PackageName, string Version) Execute(string url)
        {
            try
            {
                // Remove query params if any
                var cleanUrl = url.Split('?')[0];

                // Split by / and get last two parts
                var parts = cleanUrl.Split('/', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length < 2)
                    throw new ArgumentException("Cannot extract package info from URL");

                string version = parts[^1]; // Last part
                string packageName = parts[^2]; // Second to last

                if (string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(version))
                    throw new ArgumentException("Package name or version is empty");

                return (packageName, version);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to extract package info from URL '{url}': {ex.Message}");
            }
        }
    }
}
