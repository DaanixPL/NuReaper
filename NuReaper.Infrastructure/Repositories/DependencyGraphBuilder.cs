using System.Collections.Immutable;
using NuReaper.Application.Interfaces.Dependencies;
using Microsoft.Extensions.Logging;
using NuReaper.Infrastructure.Repositories.GraphBuilders.HelperClasses;
using NuReaper.Infrastructure.Repositories.GraphBuilders.Interfaces;
using NuReaper.Domain.Entities.Graph;

namespace NuReaper.Infrastructure.Repositories
{
    public class DependencyGraphBuilder : IDependencyGraphBuilder
    {
        private readonly HttpClient _httpClient;
        private readonly IBuildRecursiveAsync _buildRecursiveAsync;
        private readonly IBreadthFirstSearch _breadthFirstSearch;
        private readonly IDownloadAndExtractNuspecAsync _downloadAndExtractNuspecAsync;
        private readonly ILogger<DependencyGraphBuilder> _logger;
        public DependencyGraphBuilder(
            IHttpClientFactory httpClientFactory,
            IBuildRecursiveAsync buildRecursiveAsync,
            IBreadthFirstSearch breadthFirstSearch,
            IDownloadAndExtractNuspecAsync downloadAndExtractNuspecAsync,
            ILogger<DependencyGraphBuilder> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _buildRecursiveAsync = buildRecursiveAsync;
            _breadthFirstSearch = breadthFirstSearch;
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NuReaper/1.0");
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
            _logger = logger;
            _downloadAndExtractNuspecAsync = downloadAndExtractNuspecAsync;
        }
        public async Task<DependencyGraph> BuildGraphAsync(
            string rootPackageName,
            string rootPackageVersion,
            int maxDepth,
            string? targetFramework,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(rootPackageName) || string.IsNullOrEmpty(rootPackageVersion))
                throw new ArgumentException("Package name or version is missing in the URL.");

            string? nuspecPath = await _downloadAndExtractNuspecAsync.ExecuteAsync(rootPackageName, rootPackageVersion, cancellationToken);

            if (string.IsNullOrEmpty(nuspecPath))
            {
                _logger.LogError("Failed to download/extract .nuspec for {PackageName} {PackageVersion}", rootPackageName, rootPackageVersion);
                return CreateEmptyGraph(rootPackageName, rootPackageVersion);
            }

            if (Directory.Exists(nuspecPath))
            {
                var candidate = Path.Combine(nuspecPath, $"{rootPackageName}.nuspec");
                if (File.Exists(candidate))                
                {
                    nuspecPath = candidate;
                }
                else
                {
                    _logger.LogError(".nuspec not found in directory: {NuspecDirectory}", candidate);
                    return CreateEmptyGraph(rootPackageName, rootPackageVersion);
                }
            }
            else if(!File.Exists(nuspecPath))
            {
                _logger.LogError(".nuspec file not found: {NuspecPath}", nuspecPath);
                return CreateEmptyGraph(rootPackageName, rootPackageVersion);
            }

            var context = new GraphBuildingContext();
            var emptyPath = ImmutableStack<string>.Empty;

            await _buildRecursiveAsync.Execute(
                rootPackageName,
                rootPackageVersion,
                nuspecPath,
                null,
                context,
                emptyPath,
                depth: 0,
                maxDepth,
                cancellationToken);

            return new DependencyGraph
            {
                RootPackage = $"{rootPackageName}@{rootPackageVersion}",
                Nodes = context.Nodes.ToList(),
                Edges = context.Edges.ToList(),
                Cycles = context.Cycles.ToList(),
                GeneratedAt = DateTime.UtcNow
            };
        }

        public async Task<bool> HasCyclesAsync(
            string packageName,
            string version,
            string nuspecPath,
            CancellationToken cancellationToken = default)
        {
            var graph = await BuildGraphAsync(packageName, version, 20, null, cancellationToken);
            return graph.Cycles.Any();
        }

        public async Task<List<string>> FindShortestPathsAsync(
            string fromPackage,
            string toPackage,
            string nuspecPath,
            CancellationToken cancellationToken = default)
        {
            var fromParts = fromPackage.Split('@');
            
            if (fromParts.Length != 2)
                throw new ArgumentException("Format: name@version");

            var graph = await BuildGraphAsync(fromParts[0], fromParts[1], 20, null, cancellationToken);
            
            return await _breadthFirstSearch.Execute(graph, fromPackage, toPackage);
        }
        // interface?
        private DependencyGraph CreateEmptyGraph(string packageName, string version)
        {
            return new DependencyGraph
            {
                RootPackage = $"{packageName}@{version}",
                Nodes = new List<GraphNode>(),
                Edges = new List<GraphEdge>(),
                Cycles = new List<Cycle>(),
                GeneratedAt = DateTime.UtcNow
            };
        }

    }
    
}