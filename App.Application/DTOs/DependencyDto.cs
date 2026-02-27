namespace App.Application.DTOs
{
    public class DependencyDto
    {
        public required string Name { get; set; }
        public required string Version { get; set; }

        public string? TargetFramework { get; set; }
        public VersionRangeDto? VersionRange { get; set; }
        
        public required string Type { get; set; }
        public bool IsTransitive { get; set; }
    }
}
