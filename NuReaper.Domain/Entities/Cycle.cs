namespace NuReaper.Domain.Entities
{
    public class Cycle
    {
        public List<string> Path { get; set; } = new();

        public string Description => Path.Any()
            ? $"Cycle detected: {string.Join(" -> ", Path)}"
            : "Empty cycle";
        
        public int Length => Path.Count > 0 ? Path.Count -1 : 0;

        public string? StartPackage => Path.FirstOrDefault();
    }
}
