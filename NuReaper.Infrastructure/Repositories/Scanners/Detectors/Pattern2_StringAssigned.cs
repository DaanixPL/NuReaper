using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Application.DTOs;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Detectors
{
    public class Pattern2_StringAssigned : IPatternDetector
    {
        private readonly IFindNextVariableStore _findNextVariableStore;
        private readonly IFindApiCallUsingVariable _findApiCallUsingVariable;
        private readonly ICreateFinding _createFinding;
        private readonly IGetVariableName _getVariableName;
        private readonly IReconstructInterpolation _reconstructInterpolation;

        public Pattern2_StringAssigned(IFindNextVariableStore findNextVariableStore, IFindApiCallUsingVariable findApiCallUsingVariable, ICreateFinding createFinding, IGetVariableName getVariableName, IReconstructInterpolation reconstructInterpolation)
        {
            _findNextVariableStore = findNextVariableStore;
            _findApiCallUsingVariable = findApiCallUsingVariable;
            _createFinding = createFinding;
            _getVariableName = getVariableName;
            _reconstructInterpolation = reconstructInterpolation;
        }

        public bool CanDetect(Instruction instruction)
        {
            Console.WriteLine($"Pattern2\n   --> is a string load with value: \"{instruction.OpCode}\"");
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
            var nextStore = _findNextVariableStore.Execute(instructions, instructionIndex, 5);
            if (!nextStore.HasValue)
            {
                return findings;
            }
            var varName = _getVariableName.Execute(nextStore.Value.Instruction);
            var apiCallWithVar = _findApiCallUsingVariable.Execute(instructions, nextStore.Value.Index, 50);
            var interpolationResult = _reconstructInterpolation.Execute(instructions, instructionIndex);
            var stringValue = (string)instructions[instructionIndex].Operand;
            Console.WriteLine($"Pattern2_StringAssigned: Checking for API calls using string \"{stringValue}\" assigned to variable \"{varName}\" at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}");
            Console.WriteLine("-------------------------   PATTERN 2   --------------------------------");
            Console.WriteLine($"{interpolationResult.IsInterpolated} | {interpolationResult.ReconstructedString} | Concat at IL_{interpolationResult.ConcatIndex:X4}");
            Console.WriteLine("------------------------------------------------------------------------");
            if (!string.IsNullOrEmpty(apiCallWithVar))
            {
                findings.Add(_createFinding.Execute(
                    stringValue,
                    apiCallWithVar,
                    type,
                    method,
                    nextStore.Value.Index,
                    hopDepth: 1,
                    isLiteral: false,
                    flowTrace: new List<string>
                    {
                        $"String assigned to {varName} at IL_{instructionIndex:X4}",
                        $"Used in API call at IL_{nextStore.Value.Index:X4}"
                    }
                ));
                processedIndices.Add(interpolationResult.ConcatIndex); // Mark as processed
            }
            return findings;
        }
    }
}
