using dnlib.DotNet;
using dnlib.DotNet.Emit;
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
        private readonly  IFindNetworkApiCallAfterIndex _findNetworkApiCallAfterIndex;

        public Pattern4_StringInterpolation(IReconstructInterpolation reconstructInterpolation, IPatternRegistry patternRegistry ,ICreateFinding createFinding, IFindNetworkApiCallAfterIndex findNetworkApiCallAfterIndex)
        {
            _reconstructInterpolation = reconstructInterpolation;
            _patternRegistry = patternRegistry;
            _createFinding = createFinding;
            _findNetworkApiCallAfterIndex = findNetworkApiCallAfterIndex;
        }
        public bool CanDetect(Instruction instruction)
        {
           Console.WriteLine($"Pattern4\n   --> is a string load with value: \"{instruction.OpCode}\"");
            if (instruction.OpCode == OpCodes.Ldstr)
            {
                Console.WriteLine("     --> CAN BE DETECTED");
                return true;
            }
            Console.WriteLine("     --> CAN'T BE DETECTED");
            return false;
        }

        public List<FindingSummaryDto> Detect(IList<Instruction> instructions, int instructionIndex, TypeDef type, MethodDef method, HashSet<int> processedIndices)
        {
            List<FindingSummaryDto> findings = new List<FindingSummaryDto>();
            var interpolationResult = _reconstructInterpolation.Execute(instructions, instructionIndex);
            Console.WriteLine("-------------------------   PATTERN 4   --------------------------------");
            Console.WriteLine($"{interpolationResult.IsInterpolated} | {interpolationResult.ReconstructedString} | Concat at IL_{interpolationResult.ConcatIndex:X4}");
            Console.WriteLine("------------------------------------------------------------------------");
            if (interpolationResult.IsInterpolated)
            {
                var suspiciousString = _patternRegistry.IsSuspiciousString(interpolationResult.ReconstructedString);
                if (suspiciousString == ScanFindingType.None)
                {
                    return findings; // Skip if the reconstructed string is not suspicious
                }
                Console.WriteLine($"Pattern4_StringInterpolation: Detected string interpolation resulting in \"{interpolationResult.ReconstructedString}\" at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}");
                var apiCall = _findNetworkApiCallAfterIndex.Execute(instructions, interpolationResult.ConcatIndex, 50);
                Console.WriteLine($"  --> Found API call: {apiCall}");
                if (!string.IsNullOrEmpty(apiCall))
                {
                    findings.Add(_createFinding.Execute(
                        interpolationResult.ReconstructedString,
                        apiCall,
                        type,
                        method,
                        instructionIndex,
                        hopDepth: 1,
                        isLiteral: false,
                        flowTrace: new List<string> { $"constructed via string interpolation ( {suspiciousString} )" }
                    ));
                    processedIndices.Add(interpolationResult.ConcatIndex); // Mark Concat as processed
                    foreach (var idx in interpolationResult.ProcessedIndices)
                        processedIndices.Add(idx);
                }
            }
            return findings;
        }
    }
}
