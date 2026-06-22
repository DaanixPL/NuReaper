namespace NuReaper.Application.DTOs.DataFlow
{
    public class DataFlowGraphDto
    {
        public List<string> AnalyzedPackages { get; set; } = new();
        public List<DataFlowNodeDto> Nodes { get; set; } = new();
        public List<DataFlowEdgeDto> Edges { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }
}