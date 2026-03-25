namespace NuReaper.Application.DTOs.Graph
{
    public class GraphNodeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public int Depth { get; set; }
    }
}
