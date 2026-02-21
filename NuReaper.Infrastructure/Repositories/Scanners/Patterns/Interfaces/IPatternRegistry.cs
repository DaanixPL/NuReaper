using NuReaper.Domain.Enums;

namespace NuReaper.Infrastructure.Repositories.Scanners.Patterns.Interfaces
{
    public interface IPatternRegistry
    {
        public ScanFindingType IsSuspiciousString(string input);
        public bool IsSuspiciousHostname(string input);
        public bool IsPrivateIP(string input);
        public bool IsHighRiskBareApiCall(string methodFullName);
    }
}
