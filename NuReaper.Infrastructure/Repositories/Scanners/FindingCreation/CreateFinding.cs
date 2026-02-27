using dnlib.DotNet;
using Microsoft.OpenApi.Extensions;
using NuReaper.Application.DTOs;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Patterns.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.FindingCreation
{
    public class CreateFinding : ICreateFinding
    {
        private readonly IGetFindingType _getFindingType;
        private readonly IPatternRegistry _patternRegistry;
        private readonly ICalculateDangerLevel _calculateDangerLevel;
        private readonly ICalculateConfidenceScore _calculateConfidenceScore;

        public CreateFinding(IGetFindingType getFindingType, IPatternRegistry patternRegistry, ICalculateDangerLevel calculateDangerLevel, ICalculateConfidenceScore calculateConfidenceScore)
        {
            _getFindingType = getFindingType;
            _patternRegistry = patternRegistry;
            _calculateDangerLevel = calculateDangerLevel;
            _calculateConfidenceScore = calculateConfidenceScore;
        }

        public FindingSummaryDto Execute(string evidence, string? apiCall, TypeDef type, MethodDef method, int instructionIndex, int hopDepth, bool isLiteral, string flowTrace)
        {
            var findingType = _getFindingType.Execute(apiCall ?? string.Empty);
            if (findingType == ScanFindingType.Unknown)
                findingType = _patternRegistry.IsSuspiciousString(evidence);

            var dangerLevel = _calculateDangerLevel.Execute(findingType, hopDepth);
            var confidenceScore = _calculateConfidenceScore.Execute(hopDepth, isLiteral);

            return new FindingSummaryDto
            {
                Type = findingType.ToString(),
                DangerLevel = dangerLevel,
                ConfidenceScore = confidenceScore,
                Evidence = evidence,
                Location = $"{type.FullName}::{method.Name}, IL_{instructionIndex:X4}",
                RawData = $"API Call: {apiCall}",
                FlowTrace = flowTrace,
                HopDepth = hopDepth
            };
        }
    }
}
