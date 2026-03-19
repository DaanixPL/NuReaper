using System.IO.Compression;
using Microsoft.Extensions.Logging;
using NuReaper.Infrastructure.Repositories.FileHelpers.interfaces;

namespace NuReaper.Infrastructure.Repositories.FileHelpers
{
    public class ExtractNupkgAsync : IExtractNupkgAsync
    {
        private readonly ILogger<ExtractNupkgAsync> _logger;
        public ExtractNupkgAsync(ILogger<ExtractNupkgAsync> logger)
        {
            _logger = logger;
        }
        async Task<string> IExtractNupkgAsync.ExecuteAsync(string nupkgPath, CancellationToken cancellationToken)
        {
              var extractDir = Path.Combine(
                Path.GetDirectoryName(nupkgPath)!,
                Path.GetFileNameWithoutExtension(nupkgPath));

            if (Directory.Exists(extractDir))
            {
                _logger.LogInformation("Using existing extracted package: {ExtractDir}", extractDir);
                return extractDir;
            }

            _logger.LogInformation("Extracting {NupkgPath}...", nupkgPath);
            
            try
            {
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
