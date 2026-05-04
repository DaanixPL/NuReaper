using NuReaper.Domain.Entities.Graph;

namespace NuReaper.Domain.Entities
{
    public class ScanPackageResult
    {
        public required string RootPackageName { get; set; }
        public required string RootPackageVersion { get; set; }

        public float ScannedTimeAllPackages { get; set; }

        public List<Package> Packages { get; set; } = new List<Package>();
        public required DependencyGraph DependencyGraph { get; set; }
    }
}