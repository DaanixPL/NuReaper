namespace NuReaper.Domain.Entities
{
    public class GraphEdge
    {
        public string FromId { get; set; } = string.Empty;
        public string ToId { get; set; } = string.Empty;
        public string DependencyName { get; set; } = string.Empty;
        public string DependencyVersion { get; set; } = string.Empty;
        public string? TargetFramework { get; set; }
    }
}
