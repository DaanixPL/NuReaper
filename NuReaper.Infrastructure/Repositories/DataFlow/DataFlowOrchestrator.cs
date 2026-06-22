using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NuReaper.Application.Interfaces.DataFlow;
using NuReaper.Application.Interfaces.Dependencies;
using NuReaper.Domain.Entities;
using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Domain.Entities.Graph;
using NuReaper.Infrastructure.Repositories.DataFlow.DataFlowPathBuilder.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.Interfaces;
using NuReaper.Infrastructure.Repositories.FileHelpers.interfaces;
using NuReaper.Infrastructure.Repositories.FileHelpers.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Files;

namespace NuReaper.Infrastructure.Repositories.DataFlow
{
    public sealed class DataFlowOrchestrator : IDataFlowOrchestrator
    
    {
        private readonly IDependencyGraphBuilder    _dependencyGraphBuilder;
        private readonly IDownloadPackageAsync      _downloadPackageAsync;
        private readonly IExtractNupkgAsync         _extractNupkgAsync;
        private readonly IGetAssemblyFiles          _getAssemblyFiles;
        private readonly IExtractPackageInfo        _extractPackageInfo;
        private readonly IDataFlowGraphBuilder      _dataFlowGraphBuilder;
        private readonly ILogger<DataFlowOrchestrator> _logger;
        private readonly IDataFlowPathBuilder _dataFlowPathBuilder;
        private readonly ICalculateSha256 _calculateSha256;

        public DataFlowOrchestrator(
            IDependencyGraphBuilder dependencyGraphBuilder,
            IDownloadPackageAsync downloadPackageAsync,
            IExtractNupkgAsync extractNupkgAsync,
            IGetAssemblyFiles getAssemblyFiles,
            IExtractPackageInfo extractPackageInfo,
            IDataFlowGraphBuilder dataFlowGraphBuilder,
            ILogger<DataFlowOrchestrator> logger,
            IDataFlowPathBuilder dataFlowPathBuilder,
            ICalculateSha256 calculateSha256)
        {
            _dependencyGraphBuilder = dependencyGraphBuilder;
            _downloadPackageAsync = downloadPackageAsync;
            _extractNupkgAsync = extractNupkgAsync;
            _getAssemblyFiles = getAssemblyFiles;
            _extractPackageInfo = extractPackageInfo;
            _dataFlowGraphBuilder = dataFlowGraphBuilder;
            _logger = logger;
            _dataFlowPathBuilder = dataFlowPathBuilder;
            _calculateSha256 = calculateSha256;
        }

        public async Task<(DataFlowGraph, Dictionary<Package, int>)> BuildAsync(
        string rootPackageName,
        string rootPackageVersion,
        CancellationToken cancellationToken = default)
        {
            List<Package> packages = new List<Package>();

            // ── 1. Pobierz graf zależności ────────────────────────────────────────
            // graph.Nodes = root + wszystkie dependencje (rekurencyjnie do maxDepth)
            _logger.LogInformation("Building dependency graph for {RootPackageName}@{RootPackageVersion}", rootPackageName, rootPackageVersion);
            var depGraph = await _dependencyGraphBuilder.BuildGraphAsync(
                rootPackageName, rootPackageVersion, maxDepth: 20, targetFramework: null, cancellationToken);

            var allPackages = depGraph.Nodes
                .GroupBy(n => new { n.Name, n.Version })
                .Select(g => g.First())
                .ToList();

            _logger.LogInformation("Found {Count} packages (root + deps)", allPackages.Count);
            // ── 2. Dla każdej paczki: pobierz → ekstraktuj → zbierz DLL ──────────
            // Robimy to równolegle (tak jak AssemblyScanner), ale zamiast od razu skanować
            // — zbieramy (packageId, dllPath) do jednej listy.
            var inputs = new ConcurrentBag<(string packageId, string dllPath)>();
            int totalPackages = allPackages.Count;
            int processedPackages = 0;
            int failedPackages = 0;

            var semaphore = new SemaphoreSlim(
                Math.Max(1, Environment.ProcessorCount - 1));

            var tasks = allPackages.Select(async node =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var nodeUrl = $"https://www.nuget.org/api/v2/package/{node.Name}/{node.Version}";
                    var packageId = $"{node.Name}@{node.Version}";

                    string nupkgPath;
                    try
                    {
                        nupkgPath = await _downloadPackageAsync.ExecuteAsync(nodeUrl, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failedPackages);
                        _logger.LogWarning(ex, "Cannot download {Package}", packageId);
                        return;
                    }

                    var sha = _calculateSha256.Execute(nupkgPath);
                    string extractDir;
                    try
                    {
                        extractDir = await _extractNupkgAsync.ExecuteAsync(nupkgPath, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failedPackages);
                        _logger.LogWarning(ex, "Cannot extract {Package}", packageId);
                        return;
                    }

                    // GetAssemblyFiles szuka *.dll i *.exe rekurencyjnie
                    // Jedna paczka może mieć wiele DLL (net6.0/, net472/, netstandard2.0/...)
                    var dlls = _getAssemblyFiles.Execute(extractDir);

                    if (dlls.Count == 0)
                    {
                        _logger.LogDebug("No DLL files found in {Package}", packageId);
                        return;
                    }

                    foreach (var dll in dlls)
                        inputs.Add((packageId, dll));

                    // Add packages
                    var package = new Package
                    {
                        Id = Guid.NewGuid(),
                        PackageName = node.Name,
                        Version = node.Version,
                        Author = "Unknown", // TODO: Dodac z Nuget API
                        Sha256Hash = sha,
                        Downloads = 0, // TODO: Dodac z Nuget API
                        FileSize = 0, // TODO: Dodac z Nuget API
                    };
                    packages.Add(package);
                    _logger.LogDebug("Collected {Count} DLL(s) from {Package}", dlls.Count, packageId);
                }
                finally
                {
                    var done = Interlocked.Increment(ref processedPackages);
                    if (done % 10 == 0 || done == totalPackages)
                    {
                        _logger.LogInformation(
                            "DFG package progress: {Done}/{Total} processed, {Failed} failed",
                            done,
                            totalPackages,
                            failedPackages);
                    }

                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);

            var inputList = inputs.ToList();
            _logger.LogInformation(
                "Total DLL files collected: {Count} from {PkgCount} packages",
                inputList.Count, allPackages.Count);

            if (inputList.Count == 0)
            {
                _logger.LogWarning("No DLL files found — returning empty graph");
                return (new DataFlowGraph { CreatedAt = DateTime.UtcNow }, new Dictionary<Package, int>());
            }

            // ── 3. Build DataFlowGraph ───────────────────────────────────────────
            _logger.LogInformation("Building DataFlowGraph from {DllCount} DLL files...", inputList.Count);
            var (graph, packageIdToGuid) = _dataFlowGraphBuilder.Build(inputList, packages, cancellationToken);
            graph.RootPackage = $"{rootPackageName}@{rootPackageVersion}";
            graph.DependencyGraph = depGraph;

            _logger.LogInformation("DataFlowGraph built. Nodes={Nodes}, Edges={Edges}", graph.Nodes.Count, graph.Edges.Count);
            return (graph, packageIdToGuid);
        }

        public async Task<(DataFlowGraph, Dictionary<Package, int>)> BuildLocalNupkgAsync(
            string nupkgPath,
            string rootPackageName,
            string rootPackageVersion,
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(nupkgPath))
                throw new FileNotFoundException("Local .nupkg file not found.", nupkgPath);

            var sha = _calculateSha256.Execute(nupkgPath);
            var extractDir = await _extractNupkgAsync.ExecuteAsync(nupkgPath, cancellationToken);
            var dlls = _getAssemblyFiles.Execute(extractDir);
            var normalizedKey = $"{rootPackageName}@{rootPackageVersion}";

            var dependencyGraph = new DependencyGraph
            {
                RootPackage = normalizedKey,
                Nodes = new List<GraphNode>
                {
                    new GraphNode
                    {
                        Id = normalizedKey,
                        Name = rootPackageName,
                        Version = rootPackageVersion,
                        Depth = 0
                    }
                },
                Edges = new List<GraphEdge>(),
                Cycles = new List<Cycle>(),
                GeneratedAt = DateTime.UtcNow
            };

            if (dlls.Count == 0)
            {
                _logger.LogWarning("No DLL files found in local package: {Path}", nupkgPath);
                var emptyGraph = new DataFlowGraph
                {
                    RootPackage = normalizedKey,
                    DependencyGraph = dependencyGraph,
                    CreatedAt = DateTime.UtcNow
                };

                return (emptyGraph, new Dictionary<Package, int>());
            }

            var package = new Package
            {
                Id = Guid.NewGuid(),
                PackageName = rootPackageName,
                Version = rootPackageVersion,
                Author = "Local",
                Sha256Hash = sha,
                Downloads = 0,
                FileSize = new FileInfo(nupkgPath).Length
            };

            var packages = new List<Package> { package };
            var inputs = dlls.Select(dll => (packageId: package.NormalizedKey, dllPath: dll)).ToList();

            var (graph, packageToId) = _dataFlowGraphBuilder.Build(inputs, packages, cancellationToken);
            graph.RootPackage = normalizedKey;
            graph.DependencyGraph = dependencyGraph;

            _logger.LogInformation("Local DataFlowGraph built. Nodes={Nodes}, Edges={Edges}", graph.Nodes.Count, graph.Edges.Count);
            return (graph, packageToId);
        }
    }
}