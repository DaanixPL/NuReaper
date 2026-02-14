using System.IO.Compression;
using System.Security.Cryptography;
using MediatR;
using NuReaper.Application.DTOs;
using NuReaper.Application.Interfaces.Scanners;
using NuReaper.Application.Responses;

namespace NuReaper.Application.Commands.ScanPackage
{
    public class ScanPackageCommandHandler : IRequestHandler<ScanPackageCommand, ScanPackageResultResponse>
    {
        private readonly IAssemblyScanner _scanner;

        public ScanPackageCommandHandler(IAssemblyScanner scanner)
        {
            _scanner = scanner;
        }

        public async Task<ScanPackageResultResponse> Handle(ScanPackageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // 1. Parse URL to extract package info
                var (packageName, version) = ExtractPackageInfo(request.url);

                // 2. Transform URL to download link
                string urlToDownload = request.url.Replace("nuget.org/packages", "nuget.org/api/v2/package");

                //if (!urlToDownload.StartsWith("https://www.nuget.org/api/v2/package/"))
                //{
                //    throw new ArgumentException("Invalid URL format. Expected format: https://www.nuget.org/packages/{packageId}/{version}");
                //}

                // 3. Download package
                string tempFilePath = await DownloadPackageAsync(urlToDownload, cancellationToken);
                Console.WriteLine($"Package downloaded to: {tempFilePath}");

                // 4. Calculate SHA256 hash
                string sha256Hash = CalculateSha256(tempFilePath);
                Console.WriteLine($"SHA256 Hash: {sha256Hash}");

                // 5. Extract package
                string extractionPath = Path.Combine(
                    Path.GetDirectoryName(tempFilePath) ?? Directory.GetCurrentDirectory(),
                    "Extracted");

                if (Directory.Exists(extractionPath))
                    Directory.Delete(extractionPath, true);

                ZipFile.ExtractToDirectory(tempFilePath, extractionPath);
                Console.WriteLine($"Package extracted to: {extractionPath}");

                // 6. Scan package
                var result = await _scanner.ScanPackageAsync(
                    packageName,
                    version,
                    sha256Hash,
                    extractionPath,
                    cancellationToken);

                // 7. Cleanup
                try
                {
                    File.Delete(tempFilePath);
                    if (Directory.Exists(extractionPath))
                        Directory.Delete(extractionPath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Failed to cleanup temp files: {ex.Message}");
                }

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing ScanPackageCommand: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Extracts package name and version from NuGet URL
        /// URL format: https://www.nuget.org/packages/{packageName}/{version}
        /// </summary>
        private (string PackageName, string Version) ExtractPackageInfo(string url)
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

        /// <summary>
        /// Downloads NuGet package from URL
        /// </summary>
        /*
        private async Task<string> DownloadPackageAsync(string url, CancellationToken cancellationToken)
        {
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

            return tempFilePath;
        }
        */
        private async Task<string> DownloadPackageAsync(string url, CancellationToken cancellationToken)
{
    // ✅ Jeśli to local path
    if (url.StartsWith("file://") || File.Exists(url))
    {
        var filePath = url.Replace("file://", "");
        if (File.Exists(filePath))
            return filePath;
    }

    // ✅ Remote - NuGet
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

    return tempFilePath;
}

        /// <summary>
        /// Calculates SHA256 hash of a file
        /// </summary>
        private string CalculateSha256(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var fileStream = File.OpenRead(filePath))
            {
                byte[] hashBytes = sha256.ComputeHash(fileStream);
                return Convert.ToHexString(hashBytes).ToLowerInvariant();
            }
        }
    }
}