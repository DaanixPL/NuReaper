namespace NuReaper.Domain.Entities
{
    public class Scan
    {
        public Guid Id { get; set; }
        public Guid PackageId { get; set; }

        // Scan information
        public required string Version { get; set; }

        public DateTime ScanDate { get; set; } = DateTime.UtcNow;

        public float ThreatLevel { get; set; } // 1-100 (1- nothing to worry about, 100 - critical)

        private readonly List<ScanFinding> _findings = new();
        public IReadOnlyList<ScanFinding> DetectedIssues => _findings.AsReadOnly();
    }
}