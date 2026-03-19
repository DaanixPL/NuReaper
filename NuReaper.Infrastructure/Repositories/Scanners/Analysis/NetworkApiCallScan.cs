using Microsoft.Extensions.Logging;
using NuReaper.Application.DTOs;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces;
using NuReaper.Infrastructure.Repositories.FileHelpers.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;
using NuReaper.Infrastructure.Repositories.FileHelpers.interfaces;
using System.Runtime.Intrinsics.Arm;

namespace NuReaper.Infrastructure.Repositories.Scanners.Analysis
{
    public class NetworkApiCallScan : INetworkApiCallScan
    {
        private readonly IGetAssemblyFiles _getAssemblyFiles;
        private readonly IScanModule _scanModule;
        private readonly ILogger<NetworkApiCallScan> _logger;
        private readonly IDownloadPackageAsync _downloadPackageAsync;
        private readonly ICalculateSha256 _calculateSha256;
        private readonly IExtractPackageInfo _extractPackageInfo;
        private readonly IExtractNupkgAsync _extractNupkgAsync;

        public NetworkApiCallScan(IGetAssemblyFiles getAssemblyFiles, IScanModule scanModule, ILogger<NetworkApiCallScan> logger, IDownloadPackageAsync downloadPackageAsync, ICalculateSha256 calculateSha256, IExtractPackageInfo extractPackageInfo, IExtractNupkgAsync extractNupkgAsync)
        {
            _getAssemblyFiles = getAssemblyFiles;
            _scanModule = scanModule;
            _logger = logger;
            _downloadPackageAsync = downloadPackageAsync;
            _calculateSha256 = calculateSha256;
            _extractPackageInfo = extractPackageInfo;
            _extractNupkgAsync = extractNupkgAsync;
            _calculateSha256 = calculateSha256;
            _extractPackageInfo = extractPackageInfo;
        }
        public async Task<(List<FindingSummaryDto> Findings, string Sha256Hash)> Execute(string url, CancellationToken cancellationToken)
        {
            var findings = new List<FindingSummaryDto>();
            var packagePath = await _downloadPackageAsync.ExecuteAsync(url, cancellationToken);
            var sha = _calculateSha256.Execute(packagePath);

            var extractDir = await _extractNupkgAsync.ExecuteAsync(packagePath, cancellationToken);

            var files = _getAssemblyFiles.Execute(extractDir);

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    _logger.LogInformation("Scanning file: {File}", file);
                    var moduleFindings = _scanModule.Execute(file);
                    findings.AddRange(moduleFindings);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error scanning {File}", file);
                }
            }

            var (packageName, version) = _extractPackageInfo.Execute(url);

            _logger.LogInformation("Completed scanning package {PackageName} version {Version}. Total findings: {TotalFindings}", packageName, version, findings.Count);


            try
            {
                if (!string.IsNullOrEmpty(packagePath) && File.Exists(packagePath))
                    File.Delete(packagePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting file {File}", packagePath);
            }

            return await Task.FromResult((findings, sha));
        }
    }
}
