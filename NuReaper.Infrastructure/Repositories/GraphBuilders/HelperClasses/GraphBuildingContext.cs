using System.Collections.Concurrent;
using NuReaper.Domain.Entities.Graph;

namespace NuReaper.Infrastructure.Repositories.GraphBuilders.HelperClasses
{
    public class GraphBuildingContext
    {
        public ConcurrentDictionary<string, byte> Visited { get; } = new();
        public ConcurrentDictionary<string, string> NodeIdMap { get; } = new();

        private readonly ConcurrentDictionary<string, GraphNode> _nodesDict = new();
        private readonly ConcurrentDictionary<string, GraphEdge> _edgesDict = new();

        public IEnumerable<GraphNode> Nodes => _nodesDict.Values;
        public IEnumerable<GraphEdge> Edges => _edgesDict.Values;

        public ConcurrentBag<Cycle> Cycles { get; } = new();

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
        public bool TryAddNode(GraphNode node)
        {
            return _nodesDict.TryAdd(node.Id, node);
        }
        public bool TryAddEdge(GraphEdge edge)
        {
            var edgeKey = $"{edge.FromId}->{edge.ToId}";
            return _edgesDict.TryAdd(edgeKey, edge);
        }
    }
}
