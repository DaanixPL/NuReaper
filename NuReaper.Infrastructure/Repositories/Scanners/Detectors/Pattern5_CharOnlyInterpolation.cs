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
        private readonly ILogger<Pattern5_CharOnlyInterpolation> _logger;
        
        public Pattern5_CharOnlyInterpolation(IPatternRegistry patternRegistry, ICollectStackArguments collectStackArguments, IFindNextVariableStore findNextVariableStore,
            IFindApiCallUsingVariable findApiCallUsingVariable, IFindNetworkApiCallAfterIndex findNetworkApiCallAfterIndex, ICreateFinding createFinding, ILogger<Pattern5_CharOnlyInterpolation> logger)
        {
            _patternRegistry = patternRegistry;
            _collectStackArguments = collectStackArguments;
            _findNextVariableStore = findNextVariableStore;
            _findApiCallUsingVariable = findApiCallUsingVariable;
            _findNetworkApiCallAfterIndex = findNetworkApiCallAfterIndex;
            _createFinding = createFinding;
            _logger = logger;
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
            var sb = new StringBuilder();
            var method2 = instructions[instructionIndex].Operand as IMethod;
            int paramCount = method2?.MethodSig?.Params.Count ?? 0;
            
            List<FindingSummaryDto> findings = new List<FindingSummaryDto>();

            var args = _collectStackArguments.Execute(instructions, instructionIndex, paramCount);
            var reconstructed = string.Join("", args);

            sb.AppendLine($"[Pattern5] Detected potential char interpolation at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}, reconstructed value: \"{reconstructed}\"");

            var suspiciousString = _patternRegistry.IsSuspiciousString(reconstructed);
            if (string.IsNullOrEmpty(reconstructed) || suspiciousString == ScanFindingType.None)
            {
                sb.AppendLine("   --> Reconstructed string is not suspicious, skipping finding creation.");
                _logger.LogTrace(sb.ToString());
                return findings; // Skip if empty after reconstruction or not suspicious
            }
            // Check if after Concat there is a variable store + API call

            var nextStore = _findNextVariableStore.Execute(instructions, instructionIndex, 5);
            sb.AppendLine($"  --> Next variable store found at IL_{nextStore?.Instruction.Offset:X4}");
            if (nextStore.HasValue)
            {
                var apiCall = _findApiCallUsingVariable.Execute(instructions, nextStore.Value.Index, 50);
                if (!string.IsNullOrEmpty(apiCall))
                {
                    sb.AppendLine($"     --> Found API call \"{apiCall}\" using variable stored from char interpolation result.");

                    findings.Add(_createFinding.Execute(
                        reconstructed,
                        apiCall,
                        type,
                        method,
                        instructionIndex,
                        hopDepth: 1,
                        isLiteral: false,
                        flowTrace: sb.ToString()
                    ));
                    processedIndices.Add(instructionIndex); // Mark Concat as processed
                }
                else
                {
                    sb.AppendLine("     --> No API call found using variable stored from char interpolation result.");
                }
            }
            else
            {
                var apiCall = _findNetworkApiCallAfterIndex.Execute(instructions, instructionIndex);
                sb.AppendLine($"  --> Found API call: {apiCall}");
                if (!string.IsNullOrEmpty(apiCall))
                {
                    sb.AppendLine($"     --> Found API call \"{apiCall}\" directly after char interpolation result.");
                    findings.Add(_createFinding.Execute(
                        reconstructed,
                        apiCall,
                        type,
                        method,
                        instructionIndex,
                        hopDepth: 1,
                        isLiteral: false,
                        flowTrace: sb.ToString()
                    ));
                    processedIndices.Add(instructionIndex); // Mark Concat as processed
                }
                else
                {
                    sb.AppendLine("     --> No API call found directly after char interpolation result.");
                }
            }
            _logger.LogTrace(sb.ToString());
            return findings;
        }
    }
}
