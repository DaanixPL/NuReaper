using NuReaper.Domain.Entities.Graph;
using NuReaper.Domain.Enums;

namespace NuReaper.Domain.Entities.DataFlow
{
    public class DataFlowGraph
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string RootPackage { get; set; } = string.Empty;
        public Dictionary<int, string> AnalyzedPackages { get; set; } = new Dictionary<int, string>();
        public List<DataFlowNode> Nodes { get; set; } = new List<DataFlowNode>();
        public List<DataFlowEdge> Edges { get; set; } = new List<DataFlowEdge>();
        public DependencyGraph DependencyGraph { get; set; } = new DependencyGraph();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        private Dictionary<int, DataFlowNode>? _nodeLookup;
        private Dictionary<int, DataFlowEdge>? _edgeLookup;

        public Dictionary<int, DataFlowNode> NodeLookup => _nodeLookup ??= Nodes.ToDictionary(n => n.Id);
        public Dictionary<int, DataFlowEdge> EdgeLookup => _edgeLookup ??= Edges.ToDictionary(e => e.Id);

        public int TotalMethods => Nodes.Count(n => n.Type == DataFlowNodeType.Method);
        public int TotalVariables => Nodes.Count(n => n.Type == DataFlowNodeType.Variable);
        public int TotalFields => Nodes.Count(n => n.Type == DataFlowNodeType.Field);
        public int TotalCallSites => Nodes.Count(n => n.Type == DataFlowNodeType.CallSite);
        public int TotalEdges => Edges.Count;
        public int PackageId(string packageId) => AnalyzedPackages.FirstOrDefault(kv => kv.Value == packageId).Key;
    }
}