using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.DataFlow.DataFlowPathBuilder.DFS.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.DataFlowPathBuilder.Interfaces;

namespace NuReaper.Infrastructure.Repositories.DataFlow.DataFlowPathBuilder
{
    public class DataFlowPathBuilder : IDataFlowPathBuilder
    {
        private readonly IDFS _dfs;
        private readonly ILogger<DataFlowPathBuilder> _logger;

        public DataFlowPathBuilder(IDFS dfs, ILogger<DataFlowPathBuilder> logger)
        {
            _dfs = dfs;
            _logger = logger;
        }

        private static readonly HashSet<DataFlowEdgeType> StructuralEdges = new()
        {
            DataFlowEdgeType.Calls,
            DataFlowEdgeType.Targets,
            DataFlowEdgeType.Contains
        };

        public void BuildPaths(DataFlowGraph graph, Action<DataFlowPath> onPathFound, CancellationToken cancellationToken)
        {
            // === DIAGNOSTYKA ===

            // 1. Rozkład typów węzłów
            _logger.LogInformation("Graph stats: Nodes={Nodes}, Edges={Edges}",
                graph.Nodes.Count, graph.Edges.Count);

            foreach (var group in graph.Nodes.GroupBy(n => n.Type).OrderByDescending(g => g.Count()))
                _logger.LogInformation("NodeType={Type} Count={Count}", group.Key, group.Count());

            // 2. Rozkład typów krawędzi
            foreach (var group in graph.Edges.GroupBy(e => e.EdgeType).OrderByDescending(g => g.Count()))
                _logger.LogInformation("EdgeType={Type} Count={Count}", group.Key, group.Count());

            // 3. Adjacency
            var adjacency = graph.Edges
                .GroupBy(e => e.FromId)
                .ToDictionary(g => g.Key, g => g.ToList());

            // 4. Top 10 fan-out
            foreach (var x in adjacency
                .Select(kv => new
                {
                    NodeId = kv.Key,
                    Node = graph.NodeLookup.TryGetValue(kv.Key, out var n) ? n : null,
                    OutDegree = kv.Value.Count(e => !StructuralEdges.Contains(e.EdgeType))
                })
                .Where(x => x.OutDegree > 0)
                .OrderByDescending(x => x.OutDegree)
                .Take(10))
            {
                _logger.LogInformation(
                    "HighFanOut NodeId={Id} Type={Type} Name={Name} OutDegree={Out}",
                    x.NodeId, x.Node?.Type, x.Node?.Name, x.OutDegree);
            }

            // 5. Reachability – kto może dotrzeć do CallSite?
            var reverseAdj = graph.Edges
                .Where(e => !StructuralEdges.Contains(e.EdgeType))
                .GroupBy(e => e.ToId)
                .ToDictionary(g => g.Key, g => g.Select(e => e.FromId).ToList());

            var callSiteIds = graph.Nodes
                .Where(n => n.Type == DataFlowNodeType.CallSite)
                .Select(n => n.Id)
                .ToHashSet();

            var canReach = ComputeCanReachCallSite(callSiteIds, reverseAdj);

            // 6. Źródła
            var sources = graph.Nodes
                .Where(n =>
                    (n.Type == DataFlowNodeType.Literal ||
                    n.Type == DataFlowNodeType.Field ||
                    n.Type == DataFlowNodeType.ArrayElement) &&
                    canReach.Contains(n.Id))
                .ToList();

            var sourcesWithPath    = sources.Count(s => canReach.Contains(s.Id));
            var sourcesWithoutPath = sources.Count - sourcesWithPath;

            _logger.LogInformation(
                "Sources total={Total} | canReachCallSite={WithPath} | deadEnd={Dead}",
                sources.Count, sourcesWithPath, sourcesWithoutPath);

            // 7. canReach rozmiar
            _logger.LogInformation("canReach size: {Size} / {Total}", canReach.Count, graph.Nodes.Count);

            var edgesAfterFilter = adjacency.Values
                .SelectMany(edges => edges)
                .Count(e => !StructuralEdges.Contains(e.EdgeType) && canReach.Contains(e.ToId));

            _logger.LogInformation("Edges after canReach filter: {After} / {Before}",
                edgesAfterFilter,
                graph.Edges.Count(e => !StructuralEdges.Contains(e.EdgeType)));

            // 8.
            var callSitesWithReturns = graph.Edges
                .Count(e => e.EdgeType == DataFlowEdgeType.Returns &&
                            graph.NodeLookup.TryGetValue(e.FromId, out var n) &&
                            n.Type == DataFlowNodeType.CallSite);

            _logger.LogInformation("CallSites with Returns edges: {Count}", callSitesWithReturns);

            // Ile CallSite jest osiągalne z innego CallSite przez Returns?
            var callSiteToCallSitePaths = graph.Edges
                .Where(e => e.EdgeType == DataFlowEdgeType.Returns)
                .Where(e => graph.NodeLookup.TryGetValue(e.ToId, out var n) &&
                            n.Type == DataFlowNodeType.CallSite)
                .Count();

            _logger.LogInformation("Returns edges leading to another CallSite: {Count}", callSiteToCallSitePaths); 

            // 9. Cyckle
            var cycleNodes = DetectCycles(adjacency, graph.NodeLookup);
            _logger.LogInformation("Nodes participating in cycles: {Count}", cycleNodes.Count);

            // Top 10 węzłów w cyklach
            foreach (var nodeId in cycleNodes.Take(10))
            {
                if (graph.NodeLookup.TryGetValue(nodeId, out var n))
                    _logger.LogInformation("CycleNode Id={Id} Type={Type} Name={Name}", n.Id, n.Type, n.Name);
            }
            // === KONIEC DIAGNOSTYKI ===

            var parallelOptions = new ParallelOptions
            {
                CancellationToken = cancellationToken,
                MaxDegreeOfParallelism = Environment.ProcessorCount - 1
            };

            Parallel.ForEach(sources, parallelOptions, source =>
            {
                var localNodes = new List<int> { source.Id };
                var localEdges = new List<int>();
                var visited    = new HashSet<int>();
                int pathCount  = 0;

                var sw = System.Diagnostics.Stopwatch.StartNew();

                _dfs.DepthFirstSearch(source, localNodes, localEdges, visited,
                    adjacency, graph.NodeLookup,
                    path =>
                    {
                        Interlocked.Increment(ref pathCount);
                        onPathFound(path);
                    },
                    canReach, new HashSet<int>(), cancellationToken);

                sw.Stop();

                _logger.LogInformation(
                    "Source Id={Id} Type={Type} Name={Name} | Time={Ms}ms Paths={Paths} CanReach={CanReach}",
                    source.Id, source.Type, source.Name,
                    sw.ElapsedMilliseconds, pathCount,
                    canReach.Contains(source.Id));
            });
        }

        private static HashSet<int> ComputeCanReachCallSite(
            HashSet<int> callSiteIds,
            Dictionary<int, List<int>> reverseAdjacency)
        {
            var reachable = new HashSet<int>(callSiteIds);
            var queue = new Queue<int>(callSiteIds);

            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                if (!reverseAdjacency.TryGetValue(nodeId, out var predecessors))
                    continue;

                foreach (var pred in predecessors)
                    if (reachable.Add(pred))
                        queue.Enqueue(pred);
            }

            return reachable;
        }
        private static HashSet<int> DetectCycles(
    Dictionary<int, List<DataFlowEdge>> adjacency,
    Dictionary<int, DataFlowNode> nodeLookup)
{
    var visited   = new HashSet<int>();
    var inStack   = new HashSet<int>();
    var cycleNodes = new HashSet<int>();

    void Dfs(int nodeId)
    {
        visited.Add(nodeId);
        inStack.Add(nodeId);

        if (!adjacency.TryGetValue(nodeId, out var edges))
        {
            inStack.Remove(nodeId);
            return;
        }

        foreach (var edge in edges)
        {
            if (StructuralEdges.Contains(edge.EdgeType))
                continue;

            if (!visited.Contains(edge.ToId))
            {
                Dfs(edge.ToId);
            }
            else if (inStack.Contains(edge.ToId))
            {
                // cykl znaleziony
                cycleNodes.Add(edge.ToId);
                cycleNodes.Add(nodeId);
            }
        }

        inStack.Remove(nodeId);
    }

    foreach (var nodeId in nodeLookup.Keys)
        if (!visited.Contains(nodeId))
            Dfs(nodeId);

    return cycleNodes;
}
    }
 
}