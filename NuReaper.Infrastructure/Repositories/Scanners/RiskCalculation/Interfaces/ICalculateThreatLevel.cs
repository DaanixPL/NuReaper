using NuReaper.Application.DTOs;

namespace NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation.Interfaces
{
    public interface ICalculateThreatLevel
    {
        public float Execute(List<FindingSummaryDto> findings);
    }
}
