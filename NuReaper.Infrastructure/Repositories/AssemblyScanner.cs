using NuReaper.Application.Interfaces.Dependencies;
using NuReaper.Application.Interfaces.Scanners;
using NuReaper.Domain.Abstractions;
using NuReaper.Domain.Entities;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories
{
    public class AssemblyScanner : IAssemblyScanner
    {   
        private readonly INetworkApiCallScan _networkApiCallScan;
        private readonly IDependencyGraphBuilder _dependencyGraphBuilder;
        private readonly IUnitOfWork _unitOfWork;

        public AssemblyScanner(INetworkApiCallScan networkApiCallScan, IDependencyGraphBuilder dependencyGraphBuilder, IUnitOfWork unitOfWork)
        {
            _networkApiCallScan = networkApiCallScan;
            _dependencyGraphBuilder = dependencyGraphBuilder;
            _unitOfWork = unitOfWork;
        }

        public async Task<ScanPackageResult> ScanPackageAsync(string url, CancellationToken cancellationToken)
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

            ScanPackageResult resault = new ScanPackageResult
            {
                RootPackageName = rootParts[0],
                RootPackageVersion = rootParts[1],
                DependencyGraph = graph,
            };

            var cpuCount = Environment.ProcessorCount;
            var semaphore = new SemaphoreSlim(Math.Max(1, cpuCount - 1), Math.Max(1, cpuCount - 1));

            var now = DateTime.UtcNow;

            var scanTasks = uniquePackages.Select(async node =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var existingPackage = await _unitOfWork.Packages.GetPackageByNormalizedKeyAsync($"{node.Name}@{node.Version}", cancellationToken);
                    if (existingPackage != null && existingPackage.IsRecentlyScanCached())
                    {
                        return existingPackage;
                    }
                    var package = await _networkApiCallScan.Execute($"https://www.nuget.org/api/v2/package/{node.Name}/{node.Version}", cancellationToken);
                    return new Package
                    {
                        PackageName = node.Name,
                        Author = "Nuget", // dodac
                        Version = node.Version,
                        Sha256Hash = package.Sha256Hash,
                        Scans = new List<Scan>
                        {
                            new Scan
                            {
                                Version = node.Version,
                                Findings = package.Findings
                            }
                        }
                    };
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var packages = await Task.WhenAll(scanTasks).ConfigureAwait(false);
            float ScanTime = (float)(DateTime.UtcNow - startTime).TotalSeconds;

            resault.Packages.AddRange(packages);
            resault.ScannedTimeAllPackages = ScanTime;
            Console.WriteLine($"[&] findings in {ScanTime} seconds.");
            // TODO: Tutaj czysczenie pakietow skanowanych
            return resault;
        }
    }
}
