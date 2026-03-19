using System.IO.Compression;
using Microsoft.Extensions.Logging;
using NuReaper.Infrastructure.Repositories.FileHelpers.interfaces;

namespace NuReaper.Infrastructure.Repositories.FileHelpers
{
    public class ExtractNupkgAsync : IExtractNupkgAsync
    {
        private readonly ILogger<ExtractNupkgAsync> _logger;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _extractionLocks = new();

        public ExtractNupkgAsync(ILogger<ExtractNupkgAsync> logger)
        {
            _logger = logger;
        }
        async Task<string> IExtractNupkgAsync.ExecuteAsync(string nupkgPath, CancellationToken cancellationToken)
        {
            var extractDir = Path.Combine(
            Path.GetDirectoryName(nupkgPath)!,
            Path.GetFileNameWithoutExtension(nupkgPath));

            var lockKey = extractDir.ToLowerInvariant();
            var semaphore = _extractionLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                if (Directory.Exists(extractDir))
                {
                    _logger.LogInformation("Using existing extracted package: {ExtractDir}", extractDir);
                    return extractDir;
                }

                _logger.LogInformation("Extracting {NupkgPath}...", nupkgPath);
            
                await Task.Run(() => 
                    ZipFile.ExtractToDirectory(nupkgPath, extractDir, overwriteFiles: true), 
                    cancellationToken);
                
                _logger.LogInformation("Extracted to: {ExtractDir}", extractDir);
                
                return extractDir;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract {NupkgPath}", nupkgPath);
                throw;
            }
        }
    }
}
