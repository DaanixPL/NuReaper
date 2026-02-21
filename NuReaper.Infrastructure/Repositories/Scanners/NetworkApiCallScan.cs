using NuReaper.Application.DTOs;
using NuReaper.Application.Interfaces.Scanners;
using NuReaper.Application.Responses;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners
{
    public class NetworkApiCallScan : IAssemblyScanner
    {
        private readonly IGetAssemblyFiles _getAssemblyFiles;
        private readonly IScanModule _scanModule;

        public NetworkApiCallScan(IGetAssemblyFiles getAssemblyFiles, IScanModule scanModule)
        {
            _getAssemblyFiles = getAssemblyFiles;
            _scanModule = scanModule;
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
                    
                    var moduleFindings = _scanModule.Execute(file);
                    findings.AddRange(moduleFindings);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error scanning {file}: {ex.Message}");
                }
            }

            var result = new ScanPackageResultResponse
            {
                PackageName = packageName,
                Version = version,
                Author = "NuGet", // dodaj autora
                Sha256Hash = sha256Hash,
                Findings = findings,
                TotalFindings = findings.Count,
                ScannedTime = DateTime.UtcNow
            };
            return await Task.FromResult(result);
        }
    }
}
