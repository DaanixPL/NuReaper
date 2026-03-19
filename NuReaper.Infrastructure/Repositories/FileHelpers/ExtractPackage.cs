using System.IO.Compression;
using NuReaper.Infrastructure.Repositories.FileHelpers.interfaces;

namespace NuReaper.Infrastructure.Repositories.FileHelpers
{
    public class ExtractPackage : IExtractPackage
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> _extractionLocks = new();

        public async Task<string> ExecuteAsync(string tempFilePath, CancellationToken cancellationToken)
        {
            string extractDir = Path.Combine(
                Path.GetDirectoryName(tempFilePath)!,
                Path.GetFileNameWithoutExtension(tempFilePath));

            var lockKey = extractDir.ToLowerInvariant();
            var semaphore = _extractionLocks.GetOrAdd(lockKey, _ => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                if (Directory.Exists(extractDir))
                    return extractDir;

                await Task.Run(() => ZipFile.ExtractToDirectory(tempFilePath, extractDir, overwriteFiles: false), cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }

            return extractDir;
        }
    }
}
