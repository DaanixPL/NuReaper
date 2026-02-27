using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Extensions.Logging;
using NuReaper.Application.DTOs;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Patterns.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Detectors
{
    public class Pattern3_StringConstruction : IPatternDetector
    {
        private readonly ICreateFinding _createFinding;
        private readonly IFindNetworkApiCallAfterIndex _findNetworkApiCallAfterIndex;
        private readonly IPatternRegistry _patternRegistry;
        private readonly IReconstructStringFromChars _reconstructStringFromChars;
        private readonly ILogger<Pattern3_StringConstruction> _logger;

        public Pattern3_StringConstruction(ICreateFinding createFinding, IFindNetworkApiCallAfterIndex findNetworkApiCallAfterIndex, IPatternRegistry patternRegistry, IReconstructStringFromChars reconstructStringFromChars, ILogger<Pattern3_StringConstruction> logger)
        {
            _createFinding = createFinding;
            _findNetworkApiCallAfterIndex = findNetworkApiCallAfterIndex;
            _patternRegistry = patternRegistry;
            _reconstructStringFromChars = reconstructStringFromChars;
            _logger = logger;
            _patternRegistry = patternRegistry;
            _reconstructStringFromChars = reconstructStringFromChars;
        }
        public bool CanDetect(Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Ldc_I4;
        }

        public List<FindingSummaryDto> Detect(IList<Instruction> instructions, int instructionIndex, TypeDef type, MethodDef method, HashSet<int> processedIndices)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[Pattern3] Checking for string construction from character codes at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}");
            List<FindingSummaryDto> findings = new List<FindingSummaryDto>();
            var constructedString = _reconstructStringFromChars.Execute(instructions, instructionIndex);
            sb.AppendLine($"   --> Constructed string: \"{constructedString}\"");
            if (!string.IsNullOrEmpty(constructedString))
            {
                var suspiciousString = _patternRegistry.IsSuspiciousString(constructedString);
                sb.AppendLine($"      --> Constructed string \"{constructedString}\" is classified as: {suspiciousString}");
                if (suspiciousString != ScanFindingType.None)
                {
                    var apiCall = _findNetworkApiCallAfterIndex.Execute(instructions, instructionIndex);
                    sb.AppendLine($"         --> API call found after string construction: \"{apiCall}\"");
                    if (!string.IsNullOrEmpty(apiCall))
                    {
                        sb.AppendLine($"            --> Found API call \"{apiCall}\" using constructed string \"{constructedString}\" at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}");
                        findings.Add(_createFinding.Execute(
                            constructedString,
                            apiCall,
                            type,
                            method,
                            instructionIndex,
                            hopDepth: 1,
                            isLiteral: false,
                            flowTrace: sb.ToString()
                        ));
                    }
                    else
                    {
                        sb.AppendLine($"         --> No API call found after string construction.");
                    }
                }
                else
                {
                    sb.AppendLine($"         --> Constructed string is not suspicious, skipping finding creation.");
                }
                _logger.LogTrace(sb.ToString());
                return findings;
            }
            else
            {
                sb.AppendLine($"   --> No string could be reconstructed from character codes at this instruction.");
            }
            _logger.LogTrace(sb.ToString());
            return findings;
        }
    }
}
