using NuReaper.Domain.Enums;

namespace NuReaper.Domain.Entities
{
    public class ScanFinding
    {
        public Guid Id { get; set; }
        public Guid ScanId { get; set; }

        // Finding details
        public ScanFindingType Type { get; set; }

        public float DangerousLevel { get; set; } // 1-100 (1- nothing to worry about, 100 - critical)

        public string? Evidence { get; set; }
        public string? Location { get; set; } // e.g., file path, code snippet, etc.
        public string? RawData { get; set; } // optional field to store raw finding data for further analysis
    }
}