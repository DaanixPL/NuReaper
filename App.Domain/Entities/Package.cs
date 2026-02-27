using App.Domain.Entities;

namespace NuReaper.Domain.Entities
{
    public class Package
    {
        public Guid Id { get; set; }

        // Basic package information
        public required string PackageName { get; set; }
        public required string Version { get; set; }
        public required string Author { get; set; }
        public string NormalizedKey => $"{PackageName}@{Version}"; // e.g., "examplepackage@1.0.0"
        
        // Last scanning and analysis results
        public required string Sha256Hash { get; set; }

        public long Downloads { get; set; }
        public long FileSize { get; set; }

        public float ThreatLevel { get; set; } // 1-100 (1- nothing to worry about, 100 - critical)

        public DateTime LastScanDate { get; set; } = DateTime.UtcNow;

        // metadata fields for auditing and tracking
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Scans associated with this package
        public List<Scan> Scans { get; set; } = new List<Scan>();
        public List<PackageDependency> Dependencies { get; set; } = new List<PackageDependency>();
    }
}