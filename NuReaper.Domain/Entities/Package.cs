using NuReaper.Domain.Entities.Graph;

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

        public DateTime LastScanDate { get; set; } = DateTime.UtcNow;

        // metadata fields for auditing and tracking
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

        // Scans associated with this package
        public Guid LastScanId { get; set; }
        public List<Scan> Scans { get; set; } = new List<Scan>();
        public DependencyGraph? DependencyGraph { get; set; }

        public bool IsRecentlyScanCached(int cacheDaysExpiry = 7)
        {
            if (!Scans.Any())
                return false;
            
            var lastScan = Scans.OrderByDescending(s => s.ScanDate).First();
            return (DateTime.UtcNow - lastScan.ScanDate).TotalDays < cacheDaysExpiry;
        }
        public List<ScanFinding> GetLatestFindings()
        {
            return Scans
                .OrderByDescending(s => s.ScanDate)
                .FirstOrDefault()
                ?.Findings ?? new List<ScanFinding>();
        }
    }
}