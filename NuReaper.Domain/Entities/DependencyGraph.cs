namespace NuReaper.Domain.Entities
{
    public class DependencyGraph
    {
        public Guid Id { get; set; }
        
        public string RootPackage { get; set; } = string.Empty;
        public List<GraphNode> Nodes { get; set; } = new();
        public List<GraphEdge> Edges { get; set; } = new();
        public List<Cycle> Cycles { get; set; } = new();
        
        public DateTime GeneratedAt { get; set; }

        public float TotalThreatLevel { get; set; }
        public int TotalPackages => Nodes.Count;
    }
}
