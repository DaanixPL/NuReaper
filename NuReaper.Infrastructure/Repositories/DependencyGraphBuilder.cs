using App.Application.DTOs;
using App.Application.DTOs.Graph;
using App.Application.Interfaces.Dependencies;
using NuReaper.Application.Interfaces.Parsers;
using System.IO.Compression;

namespace NuReaper.Infrastructure.Repositories
{
    public class DependencyGraphBuilder : IDependencyRepository
    {
        private readonly INuspecParser _nuspecParser;
        private readonly HttpClient _httpClient;

        public DependencyGraphBuilder(
            INuspecParser nuspecParser,
            IHttpClientFactory httpClientFactory)
        {
            _nuspecParser = nuspecParser;
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("NuReaper/1.0");
        }

        /// <summary>
        /// Buduje graf zależności - prosta wersja MVP
        /// </summary>
        public async Task<DependencyGraphDto> BuildGraphAsync(
            string rootPackageName,
            string rootPackageVersion,
            int maxDepth,
            string nuspecPath,
            string? targetFramework,
            CancellationToken cancellationToken = default)
        {
            var graph = new DependencyGraphDto
            {
                RootPackage = $"{rootPackageName}@{rootPackageVersion}",
                Nodes = new List<GraphNodeDto>(),
                Edges = new List<GraphEdgeDto>(),
                Cycles = new List<CycleDto>(),
                GeneratedAt = DateTime.UtcNow
            };

            var visited = new HashSet<string>();
            var currentPath = new Stack<string>();
            var nodeIdMap = new Dictionary<string, string>();

            await BuildRecursiveAsync(
                rootPackageName,
                rootPackageVersion,
                nuspecPath,
                targetFramework,
                graph,
                visited,
                currentPath,
                nodeIdMap,
                depth: 0,
                maxDepth,
                cancellationToken);

            graph.TotalThreatLevel = 0; // Dla MVP

            return graph;
        }

        private async Task BuildRecursiveAsync(
            string packageName,
            string packageVersion,
            string nuspecPath,
            string? targetFramework,
            DependencyGraphDto graph,
            HashSet<string> visited,
            Stack<string> currentPath,
            Dictionary<string, string> nodeIdMap,
            int depth,
            int maxDepth,
            CancellationToken cancellationToken)
        {
            var packageKey = $"{packageName}@{packageVersion}";

            Console.WriteLine($"{"".PadLeft(depth * 2)}📦 {packageKey}");

            // ⛔ WARUNEK 1: Cykl
            if (currentPath.Contains(packageKey))
            {
                Console.WriteLine($"{"".PadLeft(depth * 2)}🔄 CYCLE!");
                graph.Cycles.Add(new CycleDto
                {
                    Path = currentPath.Reverse().Append(packageKey).ToList()
                });
                return;
            }

            // ⛔ WARUNEK 2: Max depth
            // if (depth > maxDepth)
            // {
            //     Console.WriteLine($"{"".PadLeft(depth * 2)}⛔ Max depth");
            //     return;
            // }

            // ⛔ WARUNEK 3: Już był
            if (visited.Contains(packageKey))
            {
                Console.WriteLine($"{"".PadLeft(depth * 2)}✓ Already visited");
                return;
            }

            visited.Add(packageKey);
            currentPath.Push(packageKey);

            try
            {
                var nodeId = Guid.NewGuid().ToString();
                nodeIdMap[packageKey] = nodeId;

                // 📊 Dodaj węzeł
                graph.Nodes.Add(new GraphNodeDto
                {
                    Id = nodeId,
                    Name = packageName,
                    Version = packageVersion,
                    ThreatLevel = 0,
                    Depth = depth,
                    IsScanned = false
                });

                // ��� KROK 2: Parsuj zależności
                List<DependencyDto> dependencies;
                try
                {
                    dependencies = await _nuspecParser.ParseDependenciesAsync(
                        nuspecPath,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"{"".PadLeft(depth * 2)}❌ Parse error: {ex.Message}");
                    return;
                }

                // 🎯 Filtruj po framework
                if (!string.IsNullOrEmpty(targetFramework))
                {
                    dependencies = dependencies
                        .Where(d => string.IsNullOrEmpty(d.TargetFramework)
                                 || d.TargetFramework.Contains(targetFramework, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                // Tylko NuGet packages (nie framework assemblies)
                dependencies = dependencies
                    .Where(d => d.Type == "NuGet")
                    .ToList();

                Console.WriteLine($"{"".PadLeft(depth * 2)}Found {dependencies.Count} dependencies");

                // 🔄 KROK 3: Rekurencja dla każdej zależności
                foreach (var dep in dependencies)
                {
                    var depKey = $"{dep.Name}@{dep.Version}";
                    var depNodeId = nodeIdMap.TryGetValue(depKey, out var existingId)
                        ? existingId
                        : Guid.NewGuid().ToString();

                    // Dodaj edge
                    graph.Edges.Add(new GraphEdgeDto
                    {
                        FromId = nodeId,
                        ToId = depNodeId,
                        DependencyName = dep.Name,
                        DependencyVersion = dep.Version,
                        TargetFramework = dep.TargetFramework
                    });

                    Console.WriteLine($"{"".PadLeft(depth * 2)}  → {depKey}");

                    // Rekurencja
                    await BuildRecursiveAsync(
                        dep.Name,
                        dep.Version,
                        nuspecPath,
                        targetFramework,
                        graph,
                        visited,
                        currentPath,
                        nodeIdMap,
                        depth + 1,
                        maxDepth,
                        cancellationToken);
                }
            }
            finally
            {
                currentPath.Pop();
            }
        }

        public async Task<bool> HasCyclesAsync(
            string packageName,
            string version,
            string nuspecPath,
            CancellationToken cancellationToken = default)
        {
            var graph = await BuildGraphAsync(packageName, version, 20, nuspecPath,null, cancellationToken);
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

            var graph = await BuildGraphAsync(fromParts[0], fromParts[1], 20, nuspecPath, null, cancellationToken);
            
            return BreadthFirstSearch(graph, fromPackage, toPackage);
        }

        private List<string> BreadthFirstSearch(DependencyGraphDto graph, string start, string target)
        {
            var queue = new Queue<(string nodeId, List<string> path)>();
            var visited = new HashSet<string>();

            var startNode = graph.Nodes.FirstOrDefault(n => $"{n.Name}@{n.Version}" == start);
            if (startNode == null) return new List<string>();

            queue.Enqueue((startNode.Id, new List<string> { start }));
            visited.Add(startNode.Id);

            while (queue.Count > 0)
            {
                var (currentId, path) = queue.Dequeue();
                var currentNode = graph.Nodes.First(n => n.Id == currentId);
                var currentKey = $"{currentNode.Name}@{currentNode.Version}";

                if (currentKey == target) return path;

                foreach (var edge in graph.Edges.Where(e => e.FromId == currentId))
                {
                    if (!visited.Contains(edge.ToId))
                    {
                        visited.Add(edge.ToId);
                        var nextNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.ToId);
                        
                        if (nextNode != null)
                        {
                            var newPath = new List<string>(path) { $"{nextNode.Name}@{nextNode.Version}" };
                            queue.Enqueue((edge.ToId, newPath));
                        }
                    }
                }
            }

            return new List<string>();
        }
    }
}