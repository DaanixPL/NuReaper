using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Extensions.Logging;
using NuReaper.Application.DTOs;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Patterns.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces
{
    public class Pattern4_StringInterpolation : IPatternDetector
    {
        private readonly IReconstructInterpolation _reconstructInterpolation;
        private readonly IPatternRegistry _patternRegistry;
        private readonly ICreateFinding _createFinding;
        private readonly IFindNetworkApiCallAfterIndex _findNetworkApiCallAfterIndex;
        private readonly ILogger<Pattern4_StringInterpolation> _logger;

        public Pattern4_StringInterpolation(IReconstructInterpolation reconstructInterpolation, IPatternRegistry patternRegistry ,ICreateFinding createFinding, IFindNetworkApiCallAfterIndex findNetworkApiCallAfterIndex, ILogger<Pattern4_StringInterpolation> logger)
        {
            _reconstructInterpolation = reconstructInterpolation;
            _patternRegistry = patternRegistry;
            _createFinding = createFinding;
            _findNetworkApiCallAfterIndex = findNetworkApiCallAfterIndex;
            _logger = logger;
        }
        public bool CanDetect(Instruction instruction)
        {
            if (instruction.OpCode == OpCodes.Ldstr)
            {
                return true;
            }
            return false;
        }

        public List<FindingSummaryDto> Detect(IList<Instruction> instructions, int instructionIndex, TypeDef type, MethodDef method, HashSet<int> processedIndices)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[Pattern4] Checking for string interpolation at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}");
            List<FindingSummaryDto> findings = new List<FindingSummaryDto>();
            var interpolationResult = _reconstructInterpolation.Execute(instructions, instructionIndex);
            sb.AppendLine($"   --> IsInterpolated: {interpolationResult.IsInterpolated} | ReconstructedString: {interpolationResult.ReconstructedString} | Concat at IL_{interpolationResult.ConcatIndex:X4}");
            if (interpolationResult.IsInterpolated)
            {
                var suspiciousString = _patternRegistry.IsSuspiciousString(interpolationResult.ReconstructedString);
                if (suspiciousString == ScanFindingType.None)
                {
                    sb.AppendLine("      --> Interpolated string is not suspicious, skipping finding creation.");
                    _logger.LogTrace(sb.ToString());
                    return findings; // Skip if the reconstructed string is not suspicious
                }
                var apiCall = _findNetworkApiCallAfterIndex.Execute(instructions, interpolationResult.ConcatIndex, 50);
                if (!string.IsNullOrEmpty(apiCall))
                {
                    sb.AppendLine($"         --> Found API call: {apiCall}");
                    findings.Add(_createFinding.Execute(
                        interpolationResult.ReconstructedString,
                        apiCall,
                        type,
                        method,
                        instructionIndex,
                        hopDepth: 1,
                        isLiteral: false,
                        flowTrace: sb.ToString()
                    ));
                    processedIndices.Add(interpolationResult.ConcatIndex); // Mark Concat as processed
                    foreach (var idx in interpolationResult.ProcessedIndices)
                        processedIndices.Add(idx);
                }
                else
                {
                    sb.AppendLine("         --> No API call found after string interpolation.");
                }
            }
            else
            {
                sb.AppendLine("   --> String is not part of an interpolation, skipping.");
            }
            _logger.LogTrace(sb.ToString());
            return findings;
        }
    }
}
