namespace NuReaper.Domain.Entities.DataFlow
{
    public class DataFlowPath
    {
        public List<int> NodesIds { get; set; } = new List<int>();
        public List<int> EdgesIds { get; set; } = new List<int>();

        public DataFlowGraph Graph { get; set; } = null!;

        public IEnumerable<DataFlowNode> Nodes =>
        NodesIds.Select(id => Graph.NodeLookup[id]);

        public IEnumerable<DataFlowEdge> Edges =>
            EdgesIds.Select(id => Graph.EdgeLookup[id]);

        public DataFlowNode Source => Graph.NodeLookup[NodesIds[0]];
        public DataFlowNode Sink => Graph.NodeLookup[NodesIds[^1]];
        public int HopCount => EdgesIds.Count;        
    }
}
