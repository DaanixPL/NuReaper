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

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (Directory.Exists(extractDir))
                {
                    _logger.LogInformation("Using existing extracted package: {ExtractDir}", extractDir);
                    return extractDir;
                }

                _logger.LogInformation("Extracting {NupkgPath}...", nupkgPath);
            
                await using (var fs = new FileStream(nupkgPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: false))
                {
                    foreach (var entry in archive.Entries)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var destinationPath = Path.Combine(extractDir, entry.FullName);
                        if (!destinationPath.StartsWith(extractDir, StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogWarning("Skipping potentially unsafe entry: {EntryName}", entry.FullName);
                            continue;
                        }

                        if (!Directory.Exists(Path.GetDirectoryName(destinationPath)))
                        {
                            var directory = Path.GetDirectoryName(destinationPath);
                            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                                Directory.CreateDirectory(directory);
                        }

                        if (string.IsNullOrEmpty(entry.Name))
                            continue;

                        await using (var entryStream = entry.Open())
                        await using (var destStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, FileOptions.Asynchronous))
                        {
                            await entryStream.CopyToAsync(destStream, 8192, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                
                _logger.LogInformation("Extracted to: {ExtractDir}", extractDir);
                
                return extractDir;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract {NupkgPath}", nupkgPath);
                throw;
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
