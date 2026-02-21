using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation
{
    public class CalculateDangerLevel : ICalculateDangerLevel
    {
        public float Execute(ScanFindingType type)
        {
            return type switch
            {
                ScanFindingType.HttpClientCall => 65f,
                ScanFindingType.WebClientCall => 70f,
                ScanFindingType.DnsCall => 55f,
                ScanFindingType.SuspiciousUrl => 60f,
                ScanFindingType.SuspiciousIpAddress => 65f,
                ScanFindingType.SuspiciousOnionAddress => 90f,
                ScanFindingType.SuspiciousBase64 => 40f,
                _ => 50f
            };
        }
    }
}
