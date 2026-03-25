using System.IO.Compression;
using Microsoft.Extensions.Hosting;
using NuReaper.Infrastructure.Repositories.FileHelpers.interfaces;
using NuReaper.Infrastructure.Repositories.FileHelpers.Interfaces;

namespace NuReaper.Infrastructure.Repositories.FileHelpers
{
    public class DownloadPackageAsync : IDownloadPackageAsync
    {
        private readonly IHostEnvironment _env;
        private readonly IExtractPackageInfo _extractPackageInfo;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SemaphoreSlim _globalLock;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _extractionLocks = new();

        public DownloadPackageAsync(IHostEnvironment env, IExtractPackageInfo extractPackageInfo, IHttpClientFactory httpClientFactory)
        {
            _env = env;
            _extractPackageInfo = extractPackageInfo;
            _httpClientFactory = httpClientFactory;
            _globalLock = new SemaphoreSlim(10, 10);
        }
        public async Task<string> ExecuteAsync(string url, CancellationToken cancellationToken)
        {
            if (_env.IsDevelopment())
            {
                if (url.StartsWith("file://") || File.Exists(url))
                {
                    var filePath = url.Replace("file://", "");
                    if (File.Exists(filePath))
                        return filePath;
                }
            }

            var lockKey = url.ToLowerInvariant();

            await _globalLock.WaitAsync(cancellationToken);
            try
            {
                var (packageName, version) = _extractPackageInfo.Execute(url);
                string fileName = $"{packageName}_{version}.nupkg";
                string tempDir = Path.Combine(Path.GetTempPath(), "NuReaperScans");
                Directory.CreateDirectory(tempDir);
                string tempFilePath = Path.Combine(tempDir, fileName);

                if (File.Exists(tempFilePath))
                {
                    try
                    {
                        var fileInfo = new FileInfo(tempFilePath);
                        if (fileInfo.Length > 0)
                            return tempFilePath;

                        File.Delete(tempFilePath);
                    }
                    catch
                    {
                        // Check?
                        return tempFilePath;
                    }
                }
                
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(url, cancellationToken);

                response.EnsureSuccessStatusCode();

                await using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous))
                {
                    await response.Content.CopyToAsync(fileStream, cancellationToken);
                }

                return tempFilePath;
            }
            finally
            {
                _globalLock.Release();
            }
        }
    }
}
