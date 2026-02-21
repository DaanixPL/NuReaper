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
    public class Pattern6_DefaultInterpolatedStringHandler : IPatternDetector
    {
        private readonly IReconstructInterpolation _reconstructInterpolation;
        private readonly IPatternRegistry _patternRegistry;
        private readonly IFindToStringAndClear _findToStringAndClear;
        private readonly IFindNextVariableStore _findNextVariableStore;
        private readonly IFindApiCallUsingVariable _findApiCallUsingVariable;
        private readonly ICreateFinding _createFinding;
        public Pattern6_DefaultInterpolatedStringHandler(IReconstructInterpolation reconstructInterpolation, IPatternRegistry patternRegistry, IFindToStringAndClear findToStringAndClear, IFindNextVariableStore findNextVariableStore, IFindApiCallUsingVariable findApiCallUsingVariable, ICreateFinding createFinding)
        {
            _reconstructInterpolation = reconstructInterpolation;
            _patternRegistry = patternRegistry;
            _findToStringAndClear = findToStringAndClear;
            _findNextVariableStore = findNextVariableStore;
            _findApiCallUsingVariable = findApiCallUsingVariable;
            _createFinding = createFinding;
        }
        public bool CanDetect(Instruction instruction)
        {
            if (instruction.OpCode == OpCodes.Call &&
                    instruction.Operand is IMethod ctor &&
                    ctor.DeclaringType?.Name == "DefaultInterpolatedStringHandler" &&
                    ctor.Name == ".ctor")
            {
                return true;
            }
            return false;
        }

        public List<FindingSummaryDto> Detect(IList<Instruction> instructions, int instructionIndex, TypeDef type, MethodDef method, HashSet<int> processedIndices)
        {
            List<FindingSummaryDto> findings = new List<FindingSummaryDto>();

            var reconstructedString = _reconstructInterpolation.Execute(instructions, instructionIndex);
            
            var suspiciousString = _patternRegistry.IsSuspiciousString(reconstructedString.ReconstructedString);
            if (string.IsNullOrEmpty(reconstructedString.ReconstructedString) || suspiciousString == ScanFindingType.None)
            {
                return findings; // Skip if we can't reconstruct or it's not suspicious
            }
            Console.WriteLine($"Pattern6_DefaultInterpolatedStringHandler: Detected DefaultInterpolatedStringHandler usage resulting in \"{reconstructedString.ReconstructedString}\" at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}");
            // Szukaj ToStringAndClear + store + API call
            var toStringIndex = _findToStringAndClear.Execute(instructions, instructionIndex);
            Console.WriteLine($"  --> Found ToStringAndClear at IL_{toStringIndex:X4}");
            if (toStringIndex > 0)
            {
                var nextStore = _findNextVariableStore.Execute(instructions, toStringIndex, 3);
                if (nextStore.HasValue)
                {
                    var apiCall = _findApiCallUsingVariable.Execute(instructions, nextStore.Value.Index, 50);
                    
                    if (!string.IsNullOrEmpty(apiCall))
                    {
                        findings.Add(_createFinding.Execute(
                            reconstructedString.ReconstructedString,
                            apiCall,
                            type,
                            method,
                            instructionIndex,
                            hopDepth: 1,
                            isLiteral: false,
                            flowTrace: new List<string> { $"constructed via DefaultInterpolatedStringHandler ( {suspiciousString} )" }
                        ));
                    }
                }
            }

            return findings;
        }
    }
}
