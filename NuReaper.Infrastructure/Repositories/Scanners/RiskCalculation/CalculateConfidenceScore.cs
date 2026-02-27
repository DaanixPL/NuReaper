using NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation
{
    public class CalculateConfidenceScore : ICalculateConfidenceScore
    {
        public float Execute(int hopDepth, bool isLiteralString)
        {
            if (isLiteralString && hopDepth == 0)
                return 95f;

            if (isLiteralString && hopDepth == 1)
                return 85f;
            
            if (!isLiteralString && hopDepth == 0)
                return 75f;
            if (hopDepth <= 2)
                return 65f;

            return Math.Max(50f, 90f - (hopDepth * 8f));
        }
    }
}
