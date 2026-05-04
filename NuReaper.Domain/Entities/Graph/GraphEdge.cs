namespace NuReaper.Domain.Entities.Graph
{
    public class GraphEdge
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FromId { get; set; } = string.Empty;
        public string ToId { get; set; } = string.Empty;
        public string DependencyName { get; set; } = string.Empty;
        public string DependencyVersion { get; set; } = string.Empty;
        public string? TargetFramework { get; set; }
    }
}
