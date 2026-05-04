using NuReaper.Application.DTOs;

namespace NuReaper.Application.Responses
{
    public record ScanPackageResultResponse
    {
        public required string RootPackageName { get; set; }
        public required string RootPackageVersion { get; set; }

        public int TotalPackages { get; set; }
        public int TotalFindingsFromAllPackages { get; set; }

        public float ScannedTimeAllPackages { get; set; } // in seconds
        public float ThreatLevelAllPackages { get; set; }

        public List<PackageDto> Packages { get; set; } = new List<PackageDto>();
        public required DependencyGraphDto DependencyGraph { get; set; }
    }
}