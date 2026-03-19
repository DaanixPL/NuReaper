using System.Globalization;
using App.Application.DTOs.Graph;
using App.Application.Interfaces.Dependencies;
using NuReaper.Application.Interfaces.Scanners;
using NuReaper.Application.Responses;
using NuReaper.Infrastructure.Repositories.FileHelpers.interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation.Interfaces;

namespace NuReaper.Infrastructure.Repositories
{
    public class AssemblyScanner : IAssemblyScanner
    {   
        private readonly INetworkApiCallScan _networkApiCallScan;
        private readonly IDependencyRepository _dependencyRepository;
        private readonly ICalculateSha256 _calculateSha256;
        private readonly ICalculateThreatLevel _calculateThreatLevel;

        public AssemblyScanner(INetworkApiCallScan networkApiCallScan, IDependencyRepository dependencyRepository, ICalculateSha256 calculateSha256, ICalculateThreatLevel calculateThreatLevel)
        {
            _networkApiCallScan = networkApiCallScan;
            _dependencyRepository = dependencyRepository;
            _calculateSha256 = calculateSha256;
            _calculateThreatLevel = calculateThreatLevel;
        }

        public async Task<ScanPackageResultResponse> ScanPackageAsync(string url, CancellationToken cancellationToken)
        {
            int maxDepth = 20; // TODO: Make this configurable
            var graph = await _dependencyRepository.BuildGraphAsync(url, maxDepth, null, cancellationToken);

            ScanPackageResultResponse resault = new ScanPackageResultResponse
            {
                RootPackageName = graph.RootPackage.Split('/')[0],
                RootPackageVersion = graph.RootPackage.Split('/')[1],
            };

            foreach (var node in graph.Nodes)
            {
               var now = DateTime.UtcNow;
                var package = await _networkApiCallScan.Execute($"https://www.nuget.org/api/v2/package/{node.Name}/{node.Version}", cancellationToken);
                resault.Packages.Add(new PackageDto
                {
                    PackageName = node.Name,
                    Author = "Nuget", // dodac
                    Version = node.Version,
                    Sha256Hash = package.Sha256Hash,
                    ThreatLevel = _calculateThreatLevel.Execute(package.Findings),
                    TotalFindings = package.Findings.Count,
                    ScannedTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc),
                });
            }
            resault.TotalPackages = resault.Packages.Count;
            resault.TotalFindingsFromAllPackages = resault.Packages.Sum(p => p.TotalFindings);
            resault.ThreatLevelAllPackages = _calculateThreatLevel.Execute(resault.Packages.SelectMany(p => p.Findings).ToList()); // lepiej to zrobic
            resault.DependencyGraph = graph;

            return resault;
        }
    }
}
