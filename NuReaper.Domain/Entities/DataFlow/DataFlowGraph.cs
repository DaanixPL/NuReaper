using NuReaper.Domain.Enums;

namespace NuReaper.Domain.Entities.DataFlow
{
    public class DataFlowGraph
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public List<string> AnalyzedPackages { get; set; } = new List<string>();
        public List<DataFlowNode> Nodes { get; set; } = new List<DataFlowNode>();
        public List<DataFlowEdge> Edges { get; set; } = new List<DataFlowEdge>();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int TotalMethods => Nodes.Count(n => n.Type == DataFlowNodeType.Method);
        public int TotalVariables => Nodes.Count(n => n.Type == DataFlowNodeType.Variable);
        public int TotalFields => Nodes.Count(n => n.Type == DataFlowNodeType.Field);
        public int TotalCallSites => Nodes.Count(n => n.Type == DataFlowNodeType.CallSite);
        public int TotalEdges => Edges.Count;
    }
}