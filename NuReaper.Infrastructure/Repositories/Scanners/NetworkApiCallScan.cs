using Microsoft.Extensions.Logging;
using NuReaper.Application.DTOs;
using NuReaper.Application.Interfaces.Scanners;
using NuReaper.Application.Responses;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners
{
    public class NetworkApiCallScan : IAssemblyScanner
    {
        private readonly IGetAssemblyFiles _getAssemblyFiles;
        private readonly IScanModule _scanModule;
        private readonly ILogger<NetworkApiCallScan> _logger;
        private readonly ICalculateThreatLevel _calculateThreatLevel;

        public NetworkApiCallScan(IGetAssemblyFiles getAssemblyFiles, IScanModule scanModule, ILogger<NetworkApiCallScan> logger, ICalculateThreatLevel calculateThreatLevel)
        {
            _getAssemblyFiles = getAssemblyFiles;
            _scanModule = scanModule;
            _logger = logger;
            _calculateThreatLevel = calculateThreatLevel;
        }
        public async Task<ScanPackageResultResponse> ScanPackageAsync(string packageName, string version, string sha256Hash, string extractedPath, CancellationToken cancellationToken)
        {
            var findings = new List<FindingSummaryDto>();
            var files = _getAssemblyFiles.Execute(extractedPath);

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

            _logger.LogInformation("Completed scanning package {PackageName} version {Version}. Total findings: {TotalFindings}", packageName, version, findings.Count);

            var result = new ScanPackageResultResponse
            {
                PackageName = packageName,
                Version = version,
                Author = "NuGet", // dodaj autora
                Sha256Hash = sha256Hash,
                Findings = findings,
                TotalFindings = findings.Count,
                ScannedTime = DateTime.UtcNow,
                ThreatLevel = _calculateThreatLevel.Execute(findings)

            };
            return await Task.FromResult(result);
        }
    }
}
