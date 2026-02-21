using NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation
{
    public class CalculateConfidenceScore : ICalculateConfidenceScore
    {
        public float Execute(int hopDepth, bool isLiteralString)
        {
             // Literal strings in API calls = highest confidence
            if (isLiteralString)
                return 95f;

            // Direct use (0 hops) = very high confidence
            if (hopDepth == 0)
                return 90f;

            // Each hop reduces confidence
            return Math.Max(50f, 90f - (hopDepth * 8f));
        }
    }
}
