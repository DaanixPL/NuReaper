using System.IO.Compression;
using Microsoft.Extensions.Hosting;
using NuReaper.Infrastructure.Repositories.FileHelpers.Interfaces;

namespace NuReaper.Infrastructure.Repositories.FileHelpers
{
    public class DownloadPackageAsync : IDownloadPackageAsync
    {
        private readonly IHostEnvironment _env;
        public DownloadPackageAsync(IHostEnvironment env)
        {
            _env = env;
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


            // Remote - NuGet
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NuReaper/1.0");

            var response = await httpClient.GetAsync(
                url,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            string fileName = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName
                ?? Path.GetFileName(url) + ".nupkg";

            string tempDir = Path.Combine(Path.GetTempPath(), "NuReaperScans");
            Directory.CreateDirectory(tempDir);

            string tempFilePath = Path.Combine(tempDir, fileName);

            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await response.Content.CopyToAsync(fileStream, cancellationToken);
            }

             return await Task.FromResult(tempFilePath);
        }
    }
}
