using App.Application.DTOs;
using App.Application.DTOs.Graph;
using App.Application.Interfaces.Dependencies;
using Microsoft.Extensions.Logging;
using NuReaper.Infrastructure.Repositories.GraphBuilders.Interfaces;

namespace NuReaper.Infrastructure.Repositories
{
    public class DependencyGraphBuilder : IDependencyRepository
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
            _logger = logger;
            _downloadAndExtractNuspecAsync = downloadAndExtractNuspecAsync;
        }
        public async Task<DependencyGraphDto> BuildGraphAsync(
            string url,
            int maxDepth,
            string? targetFramework,
            CancellationToken cancellationToken = default)
        {
            var uri = new Uri(url);
            var idx = uri.AbsolutePath.IndexOf("/package/", StringComparison.OrdinalIgnoreCase);
            if (idx < 0)
                throw new ArgumentException("Invalid URL format. Expected format: https://www.nuget.org/packages/{packageId}/{version}");
            string rootPackageName = uri.AbsolutePath.Substring(idx + "/package/".Length).TrimStart('/').Split('/')[0];
            string rootPackageVersion = uri.AbsolutePath.Substring(idx + "/package/".Length).TrimStart('/').Split('/')[1];



            var graph = new DependencyGraphDto
            {
                RootPackage = uri.AbsolutePath.Substring(idx + "/package/".Length).TrimStart('/'),
                Nodes = new List<GraphNodeDto>(),
                Edges = new List<GraphEdgeDto>(),
                Cycles = new List<CycleDto>(),
                GeneratedAt = DateTime.UtcNow
            };

            var visited = new HashSet<string>();
            var currentPath = new Stack<string>();
            var nodeIdMap = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(rootPackageName) || string.IsNullOrEmpty(rootPackageVersion))
                throw new ArgumentException("Package name or version is missing in the URL.");

            string? nuspecPath = await _downloadAndExtractNuspecAsync.ExecuteAsync(rootPackageName, rootPackageVersion, cancellationToken);

            if (string.IsNullOrEmpty(nuspecPath))
            {
                _logger.LogError("Failed to download/extract .nuspec for {PackageName} {PackageVersion}", rootPackageName, rootPackageVersion);
                return graph;
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
                    return graph;
                }
            }
            else if(!File.Exists(nuspecPath))
            {
                _logger.LogError(".nuspec file not found: {NuspecPath}", nuspecPath);
                return graph;
            }

            await _buildRecursiveAsync.Execute(
                rootPackageName,
                rootPackageVersion,
                nuspecPath,
                null,
                graph,
                visited,
                currentPath,
                nodeIdMap,
                depth: 0,
                maxDepth,
                cancellationToken);

            return graph;
        }

        public async Task<bool> HasCyclesAsync(
            string packageName,
            string version,
            string nuspecPath,
            CancellationToken cancellationToken = default)
        {
            var graph = await BuildGraphAsync($"https://www.nuget.org/packages/{packageName}/{version}", 20, null, cancellationToken);
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

            var graph = await BuildGraphAsync($"https://www.nuget.org/packages/{fromParts[0]}/{fromParts[1]}", 20, null, cancellationToken);
            
            return await _breadthFirstSearch.Execute(graph, fromPackage, toPackage);
        }
    }
}