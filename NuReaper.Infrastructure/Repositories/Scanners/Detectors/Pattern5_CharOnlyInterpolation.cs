using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Application.DTOs;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Patterns.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Detectors
{
    public class Pattern5_CharOnlyInterpolation : IPatternDetector
    {
        private readonly IPatternRegistry _patternRegistry;
        private readonly ICollectStackArguments _collectStackArguments;
        private readonly IFindNextVariableStore _findNextVariableStore;
        private readonly IFindApiCallUsingVariable _findApiCallUsingVariable;
        private readonly IFindNetworkApiCallAfterIndex _findNetworkApiCallAfterIndex;
        private readonly ICreateFinding _createFinding;
        
        public Pattern5_CharOnlyInterpolation(IPatternRegistry patternRegistry, ICollectStackArguments collectStackArguments, IFindNextVariableStore findNextVariableStore,
            IFindApiCallUsingVariable findApiCallUsingVariable, IFindNetworkApiCallAfterIndex findNetworkApiCallAfterIndex, ICreateFinding createFinding)
        {
            _patternRegistry = patternRegistry;
            _collectStackArguments = collectStackArguments;
            _findNextVariableStore = findNextVariableStore;
            _findApiCallUsingVariable = findApiCallUsingVariable;
            _findNetworkApiCallAfterIndex = findNetworkApiCallAfterIndex;
            _createFinding = createFinding;
        }

        public bool CanDetect(Instruction instruction)
        {
            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                    instruction.Operand is IMethod method2 &&
                    method2.DeclaringType?.FullName == "System.String" &&
                    method2.Name == "Concat")
            {
                return true;
            }
            return false;
        }

        public List<FindingSummaryDto> Detect(IList<Instruction> instructions, int instructionIndex, TypeDef type, MethodDef method, HashSet<int> processedIndices)
        {
            var method2 = instructions[instructionIndex].Operand as IMethod;
            int paramCount = method2?.MethodSig?.Params.Count ?? 0;
            
            List<FindingSummaryDto> findings = new List<FindingSummaryDto>();

            var args = _collectStackArguments.Execute(instructions, instructionIndex, paramCount);
            var reconstructed = string.Join("", args);

            var suspiciousString = _patternRegistry.IsSuspiciousString(reconstructed);
            if (string.IsNullOrEmpty(reconstructed) || suspiciousString == ScanFindingType.None)
            {
                return findings; // Skip if empty after reconstruction or not suspicious
            }
            // Check if after Concat there is a variable store + API call

            Console.WriteLine($"Pattern5_CharOnlyInterpolation: Detected potential char interpolation at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}, reconstructed value: \"{reconstructed}\"");
            var nextStore = _findNextVariableStore.Execute(instructions, instructionIndex, 5);
            Console.WriteLine($"  --> Next variable store found at IL_{nextStore?.Instruction.Offset:X4}");
            if (nextStore.HasValue)
            {
                var apiCall = _findApiCallUsingVariable.Execute(instructions, nextStore.Value.Index, 50);
                if (!string.IsNullOrEmpty(apiCall))
                {
                    findings.Add(_createFinding.Execute(
                        reconstructed,
                        apiCall,
                        type,
                        method,
                        instructionIndex,
                        hopDepth: 1,
                        isLiteral: false,
                        flowTrace: new List<string> { $"constructed from char interpolation ( {suspiciousString} )" }
                    ));
                    processedIndices.Add(instructionIndex); // Mark Concat as processed
                }
            }
            else
            {
                var apiCall = _findNetworkApiCallAfterIndex.Execute(instructions, instructionIndex);
                Console.WriteLine($"  --> Found API call: {apiCall}");
                if (!string.IsNullOrEmpty(apiCall))
                {
                    findings.Add(_createFinding.Execute(
                        reconstructed,
                        apiCall,
                        type,
                        method,
                        instructionIndex,
                        hopDepth: 1,
                        isLiteral: false,
                        flowTrace: new List<string> { "constructed from char interpolation (Pattern 5)" }
                    ));
                    processedIndices.Add(instructionIndex); // Mark Concat as processed
                }
            }
            return findings;
        }
    }
}
