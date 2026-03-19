using Microsoft.Extensions.Logging;
using NuReaper.Infrastructure.Repositories.GraphBuilders.Interfaces;

namespace NuReaper.Infrastructure.Repositories.GraphBuilders
{
    public class DownloadAndExtractNuspecAsync : IDownloadAndExtractNuspecAsync
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DownloadAndExtractNuspecAsync> _logger;
        public DownloadAndExtractNuspecAsync(IHttpClientFactory httpClientFactory, ILogger<DownloadAndExtractNuspecAsync> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NuReaper/1.0");
            _logger = logger;
        } 
        public async Task<string?> ExecuteAsync(string packageName, string packageVersion, CancellationToken cancellationToken)
        {
            var url = $"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/{packageVersion.ToLower()}/{packageName.ToLower()}.nuspec";
            
            try
            {
                var response = await _httpClient.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode) return null;
                
                var tempDir = Path.Combine(Path.GetTempPath(), "NuReaperDeps", $"{packageName}.{packageVersion}");
                Directory.CreateDirectory(tempDir);
                
                var nuspecPath = Path.Combine(tempDir, $"{packageName}.nuspec");
                await using var fs = File.Create(nuspecPath);
                await response.Content.CopyToAsync(fs, cancellationToken);
                
                _logger.LogTrace("Downloaded and saved .nuspec to: {NuspecPath}", nuspecPath);
                return nuspecPath;
            }
            catch { return null; }
        }
    }
}
