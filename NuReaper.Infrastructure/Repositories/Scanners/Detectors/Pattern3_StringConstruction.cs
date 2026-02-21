using dnlib.DotNet;
using dnlib.DotNet.Emit;
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

        public Pattern3_StringConstruction(ICreateFinding createFinding, IFindNetworkApiCallAfterIndex findNetworkApiCallAfterIndex, IPatternRegistry patternRegistry, IReconstructStringFromChars reconstructStringFromChars)
        {
            _createFinding = createFinding;
            _findNetworkApiCallAfterIndex = findNetworkApiCallAfterIndex;
            _patternRegistry = patternRegistry;
            _reconstructStringFromChars = reconstructStringFromChars;
        }
        public bool CanDetect(Instruction instruction)
        {
            return instruction.OpCode == OpCodes.Ldc_I4;
        }

        public List<FindingSummaryDto> Detect(IList<Instruction> instructions, int instructionIndex, TypeDef type, MethodDef method, HashSet<int> processedIndices)
        {
            List<FindingSummaryDto> findings = new List<FindingSummaryDto>();
            var constructedString = _reconstructStringFromChars.Execute(instructions, instructionIndex);
            Console.WriteLine($"Pattern3_StringConstruction: Checking for string construction from character codes at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}");
            if (!string.IsNullOrEmpty(constructedString))
            {
                var suspiciousString = _patternRegistry.IsSuspiciousString(constructedString);
                if (suspiciousString != ScanFindingType.None)
                {
                    var apiCall = _findNetworkApiCallAfterIndex.Execute(instructions, instructionIndex);
                    if (!string.IsNullOrEmpty(apiCall))
                    {
                        findings.Add(_createFinding.Execute(
                            constructedString,
                            apiCall,
                            type,
                            method,
                            instructionIndex,
                            hopDepth: 1,
                            isLiteral: false,
                            flowTrace: new List<string> { $"constructed from character codes ( {suspiciousString} )" }
                        ));
                    }
                }
                return findings;
            }
            return findings;
        }
    }
}
