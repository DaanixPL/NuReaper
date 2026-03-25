using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NuReaper.Infrastructure.Repositories.GraphBuilders.Interfaces;

namespace NuReaper.Infrastructure.Repositories.GraphBuilders
{
    public class DownloadAndExtractNuspecAsync : IDownloadAndExtractNuspecAsync
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<DownloadAndExtractNuspecAsync> _logger;

        private static readonly ConcurrentDictionary<string, string?> _cache = new();
        private readonly SemaphoreSlim _downloadSemaphore;  
    
        public DownloadAndExtractNuspecAsync(IHttpClientFactory httpClientFactory, ILogger<DownloadAndExtractNuspecAsync> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NuReaper/1.0");
            _logger = logger;
            _downloadSemaphore = new SemaphoreSlim(15, 15);
        } 
        public async Task<string?> ExecuteAsync(string packageName, string packageVersion, CancellationToken cancellationToken)
        {
            var cacheKey = $"{packageName}@{packageVersion}";

            if (_cache.TryGetValue(cacheKey, out var cachedPath))
            {
                _logger.LogTrace("Cache hit for {PackageKey}", cacheKey);
                return cachedPath;
            }

            
            await _downloadSemaphore.WaitAsync(cancellationToken);

            try
            {
                if (_cache.TryGetValue(cacheKey, out var cached))
                {
                    _logger.LogDebug("Cache hit (after wait): {PackageKey}", cacheKey);
                    return cached;
                }
                var extractPath = $"/tmp/NuReaperScans/{packageName}_{packageVersion}";

                if (Directory.Exists(extractPath))
                {
                    _logger.LogDebug("Already extracted: {PackageKey}", cacheKey);
                    _cache.TryAdd(cacheKey, extractPath);
                    return extractPath;
                }

                var url = $"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/{packageVersion.ToLower()}/{packageName.ToLower()}.nuspec";

                try
                {
                    if (_cache.TryGetValue(cacheKey, out var cachedCheck))
                    {
                        _logger.LogDebug("Cache hit (after wait): {PackageKey}", cacheKey);
                        return cachedCheck;
                    }

                    var response = await _httpClient.GetAsync(url, cancellationToken);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("HTTP {Status} for {PackageKey}", 
                            response.StatusCode, cacheKey);
                        
                        _cache.TryAdd(cacheKey, null); 
                        return null;
                    }
                    
                    var tempDir = Path.Combine(Path.GetTempPath(), "NuReaperDeps", $"{packageName}_{packageVersion}");
                    Directory.CreateDirectory(tempDir);
                    
                    var nuspecPath = Path.Combine(tempDir, $"{packageName}.nuspec");
                    await using var fs = File.Create(nuspecPath);
                    await response.Content.CopyToAsync(fs, cancellationToken);
                    
                    _logger.LogTrace("Downloaded and saved .nuspec to: {NuspecPath}", nuspecPath);
                    return nuspecPath;
                }
                catch (TaskCanceledException ex)
                {
                    _logger.LogError(ex, "Timeout downloading {PackageKey}", cacheKey);
                    _cache.TryAdd(cacheKey, null);
                    return null;
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP error downloading {PackageKey}", cacheKey);
                    _cache.TryAdd(cacheKey, null);
                    return null;
                }
            }
            finally
            {
                _downloadSemaphore.Release();
            }
        }
    }
}
