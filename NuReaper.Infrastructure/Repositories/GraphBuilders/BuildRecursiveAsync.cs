using System.Collections.Immutable;
using NuReaper.Application.DTOs;
using NuReaper.Application.DTOs.Graph;
using Microsoft.Extensions.Logging;
using NuReaper.Application.Interfaces.Parsers;
using NuReaper.Infrastructure.Repositories.GraphBuilders.HelperClasses;
using NuReaper.Infrastructure.Repositories.GraphBuilders.Interfaces;

namespace NuReaper.Infrastructure.Repositories.GraphBuilders
{
    public class BuildRecursiveAsync : IBuildRecursiveAsync
    {
        private readonly INuspecParser _nuspecParser;
        private readonly IDownloadAndExtractNuspecAsync _downloadAndExtractNuspecAsync;
        private readonly ILogger<BuildRecursiveAsync> _logger;

        private readonly SemaphoreSlim _recursionSemaphore;

        public BuildRecursiveAsync(INuspecParser nuspecParser, IDownloadAndExtractNuspecAsync downloadAndExtractNuspecAsync, ILogger<BuildRecursiveAsync> logger)
        {
            _nuspecParser = nuspecParser;
            _downloadAndExtractNuspecAsync = downloadAndExtractNuspecAsync;
            _logger = logger;
            _recursionSemaphore = new SemaphoreSlim(5, 5);
        }
        public async Task Execute(string packageName, string packageVersion, string nuspecPath, string? targetFramework, GraphBuildingContext context, ImmutableStack<string> currentPath, int depth, int maxDepth, CancellationToken cancellationToken)
        {
            var packageKey = $"{packageName}@{packageVersion}";

            if (depth <= 2)
            {
                _logger.LogInformation("Processing {PackageKey} at depth {Depth}", packageKey, depth);
            }
            else
            {
                _logger.LogTrace("{Indentation} {PackageKey}", "".PadLeft(depth * 2), packageKey);
            }


            if (depth >= maxDepth)
            {
                _logger.LogTrace("Max depth reached at {PackageKey}", packageKey);
                return;
            }

            var nodeId = context.GetOrAddNodeId(packageKey);

            context.TryAddNode(new GraphNodeDto
            {
                Id = nodeId,
                Name = packageName,
                Version = packageVersion,
                Depth = depth,
            });
            
            if (!context.TryMarkAsVisited(packageKey))
            {
                _logger.LogTrace("{PackageKey} already visited", packageKey);
                return;
            }

            if (currentPath.Contains(packageKey))
            {
                _logger.LogWarning("Cycle detected: {Path} -> {Package}",  string.Join(" -> ", currentPath), packageKey);
                context.Cycles.Add(new CycleDto
                {
                    Path = currentPath.Reverse().Reverse().ToList()
                });
                return;
            }

            var newPath = currentPath.Push(packageKey);

            string? currentNuspecPath = null;
            try
            {
                if (depth > 0)
                {
                    _logger.LogDebug("Downloading nuspec for {PackageKey}", packageKey);
                }

                if (depth == 0)
                {
                    currentNuspecPath = nuspecPath;
                }
                else
                {
                    await _recursionSemaphore.WaitAsync(cancellationToken);

                    try 
                    {
                        currentNuspecPath = await _downloadAndExtractNuspecAsync.ExecuteAsync(packageName, packageVersion, cancellationToken);
                        if (currentNuspecPath == null)
                        {
                            _logger.LogError("Failed to get .nuspec for {PackageKey}", packageKey);
                            return;
                        }
                    }
                    catch (TaskCanceledException ex)
                    {
                       _logger.LogError(ex, "Timeout downloading nuspec for {PackageKey}", packageKey);
                        return;
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogError(ex, "HTTP error downloading nuspec for {PackageKey}", packageKey);
                        return;
                    }
                    finally
                    {
                        _recursionSemaphore.Release();
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

                if (depth <= 2)
                {
                    _logger.LogInformation("{PackageKey} has {Count} dependencies", packageKey, dependencies.Count);
                }
                else
                {
                    _logger.LogTrace("{Indentation}Found {Count} dependencies", "".PadLeft(depth * 2), dependencies.Count);
                }

                var dependenciesToProcess = new List<DependencyDto>();

                foreach (var dep in dependencies)
                {
                    var depKey = $"{dep.Name}@{dep.Version}";
                    
                    var depNodeId = context.GetOrAddNodeId(depKey);

                    var edge = new GraphEdgeDto
                    {
                        FromId = nodeId,
                        ToId = depNodeId,
                        DependencyName = dep.Name,
                        DependencyVersion = dep.Version,
                        TargetFramework = dep.TargetFramework
                    };

                    context.TryAddEdge(edge);

                    _logger.LogTrace("{Indentation}  --> {DepKey}", "".PadLeft(depth * 2), depKey);

                    if (!context.IsVisited(depKey))
                    {
                      dependenciesToProcess.Add(dep);
                    }
                }

                if (dependenciesToProcess.Count > 0 && depth <= 2)
                {
                    _logger.LogInformation("Starting parallel processing of {Count} new dependencies for {PackageKey}",  dependenciesToProcess.Count, packageKey);
                }

                var tasks = dependenciesToProcess.Select(async dep =>
                {
                    _logger.LogTrace("{Indentation}Waiting to process {DepKey} of {PackageKey}", "".PadLeft(depth * 2), $"{dep.Name}@{dep.Version}", packageKey);
                    await Execute(
                        dep.Name,
                        dep.Version,
                        nuspecPath,
                        targetFramework,
                        context,
                        newPath,
                        depth + 1,
                        maxDepth,
                        cancellationToken);
                });

                await Task.WhenAll(tasks).ConfigureAwait(false);

                if (depth <= 2)
                {
                    _logger.LogInformation("Completed processing {PackageKey}", packageKey);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing {PackageKey}", packageKey);
                throw;
            }
        }
    }
}
