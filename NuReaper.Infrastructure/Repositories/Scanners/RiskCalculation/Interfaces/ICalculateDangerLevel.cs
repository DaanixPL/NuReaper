using NuReaper.Domain.Enums;

namespace NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation.Interfaces
{
    public interface ICalculateDangerLevel
    {
        public float Execute(ScanFindingType type);
    }
}
