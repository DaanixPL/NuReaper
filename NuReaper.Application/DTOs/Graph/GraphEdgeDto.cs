namespace NuReaper.Application.DTOs.Graph
{
    public class GraphEdgeDto
    {
        public string FromId { get; set; } = string.Empty;
        public string ToId { get; set; } = string.Empty;
        public string DependencyName { get; set; } = string.Empty;
        public string DependencyVersion { get; set; } = string.Empty;
        public string? TargetFramework { get; set; }
    }
}
