using NuReaper.Application.DTOs;
using NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation
{
    public class CalculateThreatLevel : ICalculateThreatLevel
    {
        public float Execute(List<FindingSummaryDto> findings)
        {
             if (findings == null || !findings.Any())
                return 0f;

            var maxDanger = findings.Max(f => f.DangerLevel) / 4f;

            var countBonus = Math.Min(30f, findings.Count * 2f);

            var uniqueTypes = findings.Select(f => f.Type).Distinct().Count();
            var diversityBonus = Math.Min(15f, uniqueTypes * 3f);

            var avgConfidence = findings.Average(f => f.ConfidenceScore);
            var confidenceBonus = avgConfidence > 90 ? 5f : 0f;

            var hasObfuscation = findings.Any(f => f.HopDepth > 0);
            var obfuscationBonus = hasObfuscation ? 10f : 0f;

            var threatLevel = Math.Min(100f, 
                maxDanger + countBonus + diversityBonus + confidenceBonus + obfuscationBonus);

            return threatLevel;
        }
    }
}
