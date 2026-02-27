using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<Pattern2_StringAssigned> _logger;

        public Pattern2_StringAssigned(IFindNextVariableStore findNextVariableStore, IFindApiCallUsingVariable findApiCallUsingVariable, ICreateFinding createFinding, IGetVariableName getVariableName, IReconstructInterpolation reconstructInterpolation, ILogger<Pattern2_StringAssigned> logger)
        {
            _findNextVariableStore = findNextVariableStore;
            _findApiCallUsingVariable = findApiCallUsingVariable;
            _createFinding = createFinding;
            _getVariableName = getVariableName;
            _reconstructInterpolation = reconstructInterpolation;
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
            sb.AppendLine($"[Pattern2] Checking for API calls using string \"{stringValue}\" assigned to variable \"{varName}\" at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}");
            sb.AppendLine($"   --> {interpolationResult.IsInterpolated} | {interpolationResult.ReconstructedString} | Concat at IL_{interpolationResult.ConcatIndex:X4}");
            if (!string.IsNullOrEmpty(apiCallWithVar))
            {
                if (processedIndices.Contains(nextStore.Value.Index) || processedIndices.Contains(instructionIndex))
                {
                    sb.AppendLine($"         --> Skipping already processed instruction at IL_{nextStore.Value.Index:X4} or IL_{instructionIndex:X4}");
                    _logger.LogTrace(sb.ToString());
                    return findings;
                }
                sb.AppendLine($"         --> Found API call \"{apiCallWithVar}\" using variable \"{varName}\" assigned from string \"{stringValue}\" at IL_{nextStore.Value.Index:X4} in {type.FullName}::{method.Name}");
                findings.Add(_createFinding.Execute(
                    stringValue,
                    apiCallWithVar,
                    type,
                    method,
                    nextStore.Value.Index,
                    hopDepth: 1,
                    isLiteral: false,
                    flowTrace: sb.ToString()
                ));
                foreach (var idx in interpolationResult.ProcessedIndices)
                {
                    processedIndices.Add(idx); // Mark all Ldstr in interpolation as processed
                }
                processedIndices.Add(interpolationResult.ConcatIndex); // Mark as processed
            }
            else
            {
                sb.AppendLine($"         --> No API call found for variable \"{varName}\" assigned from string \"{stringValue}\" at IL_{nextStore.Value.Index:X4} in {type.FullName}::{method.Name}");
            }
            _logger.LogTrace(sb.ToString());
            return findings;
        }
    }
}
