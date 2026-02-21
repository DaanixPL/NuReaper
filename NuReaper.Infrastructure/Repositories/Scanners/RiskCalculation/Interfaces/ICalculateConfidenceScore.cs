namespace NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation.Interfaces
{
    public interface ICalculateConfidenceScore
    {
        public float Execute(int hopDepth, bool isLiteralString);
    }
}
