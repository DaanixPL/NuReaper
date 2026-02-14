using NuReaper.Domain.Enums;

namespace NuReaper.Domain.Entities
{
    public class ScanFinding
    {
        public Guid Id { get; set; }
        public Guid ScanId { get; set; }

        // Finding details
        public ScanFindingType Type { get; set; }

        public float ConfidenceScore { get; set; } // 0-100 (0 - very uncertain, 100 - very confident)
        public float DangerLevel { get; set; } // 1-100 (1- nothing to worry about, 100 - critical)

        public string? Evidence { get; set; }
        public string? Location { get; set; } // e.g., file path, code snippet, etc.
        public string? RawData { get; set; } // optional field to store raw finding data for further analysis

         public List<string> FlowTrace { get; set; } = new();  // ["Method1", "Method2", "Method3"]
        public int HopDepth { get; set; }
    }
}