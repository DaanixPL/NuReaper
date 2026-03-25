using System.Collections.Concurrent;
using System.Collections.Immutable;
using NuReaper.Application.DTOs.Graph;

namespace NuReaper.Infrastructure.Repositories.GraphBuilders.HelperClasses
{
    public class GraphBuildingContext
    {
        public ConcurrentDictionary<string, byte> Visited { get; } = new();
        public ConcurrentDictionary<string, string> NodeIdMap { get; } = new();

        private readonly ConcurrentDictionary<string, GraphNodeDto> _nodesDict = new();
        private readonly ConcurrentDictionary<string, GraphEdgeDto> _edgesDict = new();

        public IEnumerable<GraphNodeDto> Nodes => _nodesDict.Values;
        public IEnumerable<GraphEdgeDto> Edges => _edgesDict.Values;

        public ConcurrentBag<CycleDto> Cycles { get; } = new();

        public bool TryMarkAsVisited(string packageKey)
        {
            return Visited.TryAdd(packageKey, 0);
        }
        public bool IsVisited(string packageKey)
        {
            return Visited.ContainsKey(packageKey);
        }
        public string GetOrAddNodeId(string packageKey)
        {
            return NodeIdMap.GetOrAdd(packageKey, _ => Guid.NewGuid().ToString());
        }
        public bool TryAddNode(GraphNodeDto node)
        {
            return _nodesDict.TryAdd(node.Id, node);
        }
        public bool TryAddEdge(GraphEdgeDto edge)
        {
            var edgeKey = $"{edge.FromId}->{edge.ToId}";
            return _edgesDict.TryAdd(edgeKey, edge);
        }
    }
}
