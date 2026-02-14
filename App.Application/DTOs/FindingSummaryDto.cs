using NuReaper.Domain.Enums;

namespace NuReaper.Application.DTOs
{
    public class FindingSummaryDto
    {
        public ScanFindingType Type { get; set; }
        
        public float ConfidenceScore { get; set; }
        public float DangerLevel { get; set; }

        public string? Evidence { get; set; }
        public string? Location { get; set; }
        public string? RawData { get; set; }

        public int HopDepth { get; set; }

        public List<string>? FlowTrace { get; set; }
    }
}