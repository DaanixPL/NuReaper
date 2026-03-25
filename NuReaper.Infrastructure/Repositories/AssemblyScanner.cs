using NuReaper.Application.DTOs;
using NuReaper.Application.Interfaces.Dependencies;
using NuReaper.Application.Interfaces.Scanners;
using NuReaper.Application.Responses;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation.Interfaces;

namespace NuReaper.Infrastructure.Repositories
{
    public class AssemblyScanner : IAssemblyScanner
    {   
        private readonly INetworkApiCallScan _networkApiCallScan;
        private readonly IDependencyGraphBuilder _dependencyGraphBuilder;
        private readonly ICalculateThreatLevel _calculateThreatLevel;

        public AssemblyScanner(INetworkApiCallScan networkApiCallScan, IDependencyGraphBuilder dependencyGraphBuilder, ICalculateThreatLevel calculateThreatLevel)
        {
            _networkApiCallScan = networkApiCallScan;
            _dependencyGraphBuilder = dependencyGraphBuilder;
            _calculateThreatLevel = calculateThreatLevel;
        }

        public async Task<ScanPackageResultResponse> ScanPackageAsync(string url, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            // TODO: Mozna dodac cache wynikow + dodawanie do db. Oraz zapis w db czas skanowania.
            int maxDepth = 20; // TODO: Make this configurable
            var graph = await _dependencyGraphBuilder.BuildGraphAsync(url, maxDepth, null, cancellationToken);

            var uniquePackages = graph.Nodes.GroupBy(n => new {n.Name, n.Version}).Select(g => g.First()).ToList();

            var rootParts = graph.RootPackage.Split('@');

            if (rootParts.Length != 2)
            {
                throw new InvalidOperationException($"Invalid RootPackage format: {graph.RootPackage}. Expected format: 'name@version'");
            }

            ScanPackageResultResponse resault = new ScanPackageResultResponse
            {
                RootPackageName = rootParts[0],
                RootPackageVersion = rootParts[1],
            };

            var cpuCount = Environment.ProcessorCount;
            var semaphore = new SemaphoreSlim(Math.Max(1, cpuCount - 1), Math.Max(1, cpuCount - 1));

            var now = DateTime.UtcNow;

            var scanTasks = uniquePackages.Select(async node =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var package = await _networkApiCallScan.Execute($"https://www.nuget.org/api/v2/package/{node.Name}/{node.Version}", cancellationToken);
                    return new PackageDto
                    {
                        PackageName = node.Name,
                        Author = "Nuget", // dodac
                        Version = node.Version,
                        Sha256Hash = package.Sha256Hash,
                        ThreatLevel = _calculateThreatLevel.Execute(package.Findings),
                        Findings = package.Findings,
                        TotalFindings = package.Findings.Count,
                        ScannedTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc),
                    };
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var packages = await Task.WhenAll(scanTasks).ConfigureAwait(false);
            resault.Packages.AddRange(packages);

            resault.TotalPackages = resault.Packages.Count;
            resault.TotalFindingsFromAllPackages = resault.Packages.Sum(p => p.TotalFindings);
            resault.ThreatLevelAllPackages = _calculateThreatLevel.Execute(resault.Packages.SelectMany(p => p.Findings).ToList()); // lepiej to zrobic
            resault.DependencyGraph = graph;
            resault.ScannedTimeAllPackages = DateTime.UtcNow;

            Console.WriteLine($"[&] findings in {(DateTime.UtcNow - startTime).TotalSeconds} seconds.");
            // TODO: Tutaj czysczenie pakietow skanowanych
            return resault;
        }
    }
}
