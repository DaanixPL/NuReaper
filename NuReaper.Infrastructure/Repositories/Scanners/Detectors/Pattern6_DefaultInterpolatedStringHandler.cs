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
    public class Pattern6_DefaultInterpolatedStringHandler : IPatternDetector
    {
        private readonly IReconstructFromInterpolatedHandler _reconstructFromInterpolatedHandler;
        private readonly IPatternRegistry _patternRegistry;
        private readonly IFindToStringAndClear _findToStringAndClear;
        private readonly IFindNextVariableStore _findNextVariableStore;
        private readonly IFindApiCallUsingVariable _findApiCallUsingVariable;
        private readonly ICreateFinding _createFinding;
        private readonly ILogger<Pattern6_DefaultInterpolatedStringHandler> _logger;
        public Pattern6_DefaultInterpolatedStringHandler(IReconstructFromInterpolatedHandler reconstructFromInterpolatedHandler, IPatternRegistry patternRegistry, IFindToStringAndClear findToStringAndClear, IFindNextVariableStore findNextVariableStore, IFindApiCallUsingVariable findApiCallUsingVariable, ICreateFinding createFinding, ILogger<Pattern6_DefaultInterpolatedStringHandler> logger)
        {
            _reconstructFromInterpolatedHandler = reconstructFromInterpolatedHandler;
            _patternRegistry = patternRegistry;
            _findToStringAndClear = findToStringAndClear;
            _findNextVariableStore = findNextVariableStore;
            _findApiCallUsingVariable = findApiCallUsingVariable;
            _createFinding = createFinding;
            _logger = logger;
        }
        public bool CanDetect(Instruction instruction)
        {
            if (instruction.OpCode == OpCodes.Call &&
                    instruction.Operand is IMethod ctor &&
                    ctor.DeclaringType?.FullName?.Contains("DefaultInterpolatedStringHandler") == true &&
                    ctor.Name == ".ctor")
            {
                return true;
            }
            return false;
        }

        public List<FindingSummaryDto> Detect(IList<Instruction> instructions, int instructionIndex, TypeDef type, MethodDef method, HashSet<int> processedIndices)
        {
            var sb = new StringBuilder();
            List<FindingSummaryDto> findings = new List<FindingSummaryDto>();

            var reconstructedString = _reconstructFromInterpolatedHandler.Execute(instructions, instructionIndex);
            sb.AppendLine($"[Pattern 6] Reconstructed string: \"{reconstructedString}\"");
            var suspiciousString = _patternRegistry.IsSuspiciousString(reconstructedString);
            if (string.IsNullOrEmpty(reconstructedString) || suspiciousString == ScanFindingType.None)
            {
                sb.AppendLine("   --> Reconstructed string is not suspicious, skipping finding creation.");
                _logger.LogTrace(sb.ToString());
                return findings; // Skip if we can't reconstruct or it's not suspicious
            }
            // Szukaj ToStringAndClear + store + API call
            var toStringIndex = _findToStringAndClear.Execute(instructions, instructionIndex, processedIndices);
            sb.AppendLine($"  --> Found ToStringAndClear at IL_{toStringIndex:X4}");
            if (toStringIndex > 0)
            {
                sb.AppendLine($"     --> Found ToStringAndClear at IL_{toStringIndex:X4}, looking for subsequent variable store and API call...");
                var nextStore = _findNextVariableStore.Execute(instructions, toStringIndex, 3);
                if (nextStore.HasValue)
                {
                    sb.AppendLine("        --> Found variable store after ToStringAndClear, looking for API calls using this variable...");
                    var apiCall = _findApiCallUsingVariable.Execute(instructions, nextStore.Value.Index, 50);
                    
                    if (!string.IsNullOrEmpty(apiCall))
                    {
                        sb.AppendLine($"           --> Found API call \"{apiCall}\" using variable stored from ToStringAndClear result.");
                        findings.Add(_createFinding.Execute(
                            reconstructedString,
                            apiCall,
                            type,
                            method,
                            instructionIndex,
                            hopDepth: 1,
                            isLiteral: false,
                            flowTrace: sb.ToString()
                        ));
                        processedIndices.Add(toStringIndex);
                        processedIndices.Add(nextStore.Value.Index);
                    }
                    else
                    {
                        sb.AppendLine("           --> No API call found using variable stored from ToStringAndClear result.");
                    }
                }
                else
                {
                    sb.AppendLine("        --> No variable store found after ToStringAndClear.");
                }
            }
            else
            {
                sb.AppendLine("     --> No ToStringAndClear call found after DefaultInterpolatedStringHandler constructor.");
            }
            _logger.LogTrace(sb.ToString());
            return findings;
        }
    }
    
}

