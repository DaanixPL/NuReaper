using NuReaper.Application.DTOs.Graph;

namespace NuReaper.Application.DTOs
{
    public class DependencyGraphDto
    {
        public string RootPackage { get; set; } = string.Empty;
        public List<GraphNodeDto> Nodes { get; set; } = new();
        public List<GraphEdgeDto> Edges { get; set; } = new();
        public List<CycleDto> Cycles { get; set; } = new();
        
        public DateTime GeneratedAt { get; set; }

        public float TotalThreatLevel { get; set; }
        public int TotalPackages => Nodes.Count;
    }
}
