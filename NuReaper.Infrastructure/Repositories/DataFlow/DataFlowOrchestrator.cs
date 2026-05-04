using Microsoft.Extensions.Logging;
using NuReaper.Application.DTOs.DataFlow;
using NuReaper.Application.Interfaces.DataFlow;
using NuReaper.Application.Interfaces.Dependencies;
using NuReaper.Infrastructure.Repositories.DataFlow.Interfaces;
using NuReaper.Infrastructure.Repositories.FileHelpers.interfaces;
using NuReaper.Infrastructure.Repositories.FileHelpers.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;

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

        public DataFlowOrchestrator(
            IDependencyGraphBuilder dependencyGraphBuilder,
            IDownloadPackageAsync downloadPackageAsync,
            IExtractNupkgAsync extractNupkgAsync,
            IGetAssemblyFiles getAssemblyFiles,
            IExtractPackageInfo extractPackageInfo,
            IDataFlowGraphBuilder dataFlowGraphBuilder,
            ILogger<DataFlowOrchestrator> logger)
        {
            _dependencyGraphBuilder = dependencyGraphBuilder;
            _downloadPackageAsync = downloadPackageAsync;
            _extractNupkgAsync = extractNupkgAsync;
            _getAssemblyFiles = getAssemblyFiles;
            _extractPackageInfo = extractPackageInfo;
            _dataFlowGraphBuilder = dataFlowGraphBuilder;
            _logger = logger;
        }

        public async Task<DataFlowGraphDto> BuildAsync(
        string rootPackageUrl,
        CancellationToken cancellationToken = default)
        {
            // ── 1. Pobierz graf zależności ────────────────────────────────────────
            // graph.Nodes = root + wszystkie dependencje (rekurencyjnie do maxDepth)
            _logger.LogInformation("Building dependency graph for {Url}", rootPackageUrl);
            var depGraph = await _dependencyGraphBuilder.BuildGraphAsync(
                rootPackageUrl, maxDepth: 20, targetFramework: null, cancellationToken);

            var allPackages = depGraph.Nodes
                .GroupBy(n => new { n.Name, n.Version })
                .Select(g => g.First())
                .ToList();

            _logger.LogInformation("Found {Count} packages (root + deps)", allPackages.Count);

            // ── 2. Dla każdej paczki: pobierz → ekstraktuj → zbierz DLL ──────────
            // Robimy to równolegle (tak jak AssemblyScanner), ale zamiast od razu skanować
            // — zbieramy (packageName, dllPath) do jednej listy.
            var inputs = new System.Collections.Concurrent.ConcurrentBag<(string packageName, string dllPath)>();

            var semaphore = new SemaphoreSlim(
                Math.Max(1, Environment.ProcessorCount - 1));

            var tasks = allPackages.Select(async node =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var nodeUrl     = $"https://www.nuget.org/api/v2/package/{node.Name}/{node.Version}";
                    // "Newtonsoft.Json@13.0.1" — ten sam format co NormalizedKey w Package.cs
                    var packageName = $"{node.Name}@{node.Version}";

                    string nupkgPath;
                    try
                    {
                        nupkgPath = await _downloadPackageAsync.ExecuteAsync(nodeUrl, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Cannot download {Package}", packageName);
                        return;
                    }

                    string extractDir;
                    try
                    {
                        extractDir = await _extractNupkgAsync.ExecuteAsync(nupkgPath, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Cannot extract {Package}", packageName);
                        return;
                    }

                    // GetAssemblyFiles szuka *.dll i *.exe rekurencyjnie
                    // Jedna paczka może mieć wiele DLL (net6.0/, net472/, netstandard2.0/...)
                    var dlls = _getAssemblyFiles.Execute(extractDir);

                    if (dlls.Count == 0)
                    {
                        _logger.LogDebug("No DLL files found in {Package}", packageName);
                        return;
                    }

                    foreach (var dll in dlls)
                        inputs.Add((packageName, dll));

                    _logger.LogDebug("Collected {Count} DLL(s) from {Package}", dlls.Count, packageName);
                }
                finally
                {
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
                return new DataFlowGraphDto { GeneratedAt = DateTime.UtcNow };
            }

            // ── 3. Zbuduj DataFlowGraph ───────────────────────────────────────────
            // WSZYSTKIE dll razem — resolver cross-package wymaga wspólnego ModuleContext.
            _logger.LogInformation("Building DataFlowGraph...");
            return _dataFlowGraphBuilder.Build(inputList, cancellationToken);
        }
    }
}