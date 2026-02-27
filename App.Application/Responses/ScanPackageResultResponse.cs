using App.Application.DTOs;
using NuReaper.Application.DTOs;

namespace NuReaper.Application.Responses
{
    public record ScanPackageResultResponse
    {
        public required string PackageName { get; set; }
        public required string Version { get; set; }
        public required string Author { get; set; }

        public required string Sha256Hash { get; set; }

        public long Downloads { get; set; }
        public long FileSize { get; set; }

        public float ThreatLevel { get; set; }

        public int TotalFindings { get; set; }

        public DateTime ScannedTime { get; set; }

        public List<FindingSummaryDto> Findings { get; set; } = new List<FindingSummaryDto>();
        public List<DependencyDto> Dependencies { get; set; } = new List<DependencyDto>();
    }
}