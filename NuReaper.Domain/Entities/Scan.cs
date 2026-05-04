namespace NuReaper.Domain.Entities
{
    public class Scan
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid PackageId { get; set; }

        // Scan information
        public required string Version { get; set; }

        public DateTime ScanDate { get; set; } = DateTime.UtcNow;

        public float ThreatLevel { get; set; } // 1-100 (1- nothing to worry about, 100 - critical)

        public List<ScanFinding> Findings = new();
    }
}