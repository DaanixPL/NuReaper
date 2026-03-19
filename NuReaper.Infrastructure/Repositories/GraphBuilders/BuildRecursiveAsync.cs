using App.Application.DTOs;
using App.Application.DTOs.Graph;
using Microsoft.Extensions.Logging;
using NuReaper.Application.Interfaces.Parsers;
using NuReaper.Infrastructure.Repositories.GraphBuilders.Conditions.Interfaces;
using NuReaper.Infrastructure.Repositories.GraphBuilders.Interfaces;

namespace NuReaper.Infrastructure.Repositories.GraphBuilders
{
    public class BuildRecursiveAsync : IBuildRecursiveAsync
    {
        private readonly INuspecParser _nuspecParser;
        private readonly IConditions _conditions;
        private readonly IDownloadAndExtractNuspecAsync _downloadAndExtractNuspecAsync;
        private readonly ILogger<BuildRecursiveAsync> _logger;
        public BuildRecursiveAsync(INuspecParser nuspecParser, IConditions conditions, IDownloadAndExtractNuspecAsync downloadAndExtractNuspecAsync, ILogger<BuildRecursiveAsync> logger)
        {
            _nuspecParser = nuspecParser;
            _conditions = conditions;
            _downloadAndExtractNuspecAsync = downloadAndExtractNuspecAsync;
            _logger = logger;
        }
        public async Task Execute(string packageName, string packageVersion, string nuspecPath, string? targetFramework, DependencyGraphDto graph, HashSet<string> visited, Stack<string> currentPath, Dictionary<string, string> nodeIdMap, int depth, int maxDepth, CancellationToken cancellationToken)
        {
            var packageKey = $"{packageName}@{packageVersion}";

            _logger.LogTrace("{Indentation} {PackageKey}", "".PadLeft(depth * 2), packageKey);

            var nodeId = nodeIdMap.TryGetValue(packageKey, out var existingNodeId)
                ? existingNodeId
                : Guid.NewGuid().ToString();
            
            if (!nodeIdMap.ContainsKey(packageKey))
            {
                nodeIdMap[packageKey] = nodeId;
            }

            if (!graph.Nodes.Any(n => n.Id == nodeId))
            {
                graph.Nodes.Add(new GraphNodeDto
                {
                    Id = nodeId,
                    Name = packageName,
                    Version = packageVersion,
                    ThreatLevel = 0,
                    Depth = depth,
                    IsScanned = false
                });
            }
            

            // Conditions  

            _conditions.IsCycle(currentPath, packageKey, graph);

            if (_conditions.IsMaxDepth(depth, maxDepth)) return;

            if (_conditions.IsVisited(visited, packageKey)) return;

            visited.Add(packageKey);
            currentPath.Push(packageKey);

            string? currentNuspecPath = null;
            try
            {
                if (depth == 0)
                {
                    currentNuspecPath = nuspecPath;
                }
                else
                {
                    currentNuspecPath = await _downloadAndExtractNuspecAsync.ExecuteAsync(packageName, packageVersion, cancellationToken);
                    if (currentNuspecPath == null)
                    {
                        _logger.LogError("Failed to get .nuspec for {PackageKey}", packageKey);
                        return;
                    }
                }

                List<DependencyDto> dependencies;
                try
                {
                    dependencies = await _nuspecParser.ParseDependenciesAsync(
                        currentNuspecPath,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Parse error for {PackageKey}", packageKey);
                    return;
                }

                if (!string.IsNullOrEmpty(targetFramework))
                {
                    dependencies = dependencies
                        .Where(d => string.IsNullOrEmpty(d.TargetFramework)
                                || d.TargetFramework.Contains(targetFramework, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                
                dependencies = dependencies
                    .Where(d => d.Type == "NuGet")
                    .ToList();

                _logger.LogTrace("{Indentation}Found {Count} dependencies", "".PadLeft(depth * 2), dependencies.Count);

                foreach (var dep in dependencies)
                {
                    var depKey = $"{dep.Name}@{dep.Version}";
                    
                    var depNodeId = nodeIdMap.TryGetValue(depKey, out var existingId)
                        ? existingId
                        : Guid.NewGuid().ToString();

                    if (!nodeIdMap.ContainsKey(depKey))
                    {
                        nodeIdMap[depKey] = depNodeId;
                    }

                    var edge = new GraphEdgeDto
                    {
                        FromId = nodeId,
                        ToId = depNodeId,
                        DependencyName = dep.Name,
                        DependencyVersion = dep.Version,
                        TargetFramework = dep.TargetFramework
                    };

                    if (!graph.Edges.Any(e => e.FromId == edge.FromId && e.ToId == edge.ToId))
                    {
                        graph.Edges.Add(edge);
                    }

                    _logger.LogTrace("{Indentation}  --> {DepKey}", "".PadLeft(depth * 2), depKey);

                    if (!visited.Contains(depKey))
                    {
                        await Execute(
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
            }
            finally
            {
                currentPath.Pop();
            }
        }
    }
}
