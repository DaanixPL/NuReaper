namespace App.Domain.Entities
{
    public class PackageDependency
    {
        public Guid Id { get; set; }
        public Guid PackageId { get; set; }

        public required string Name { get; set; }
        public required string Version { get; set; }

        public string? TargetFramework { get; set; }
        public VersionRange? VersionRange { get; set; }

        public required string Type { get; set; }
        public bool IsTransitive { get; set; }
    }
}
