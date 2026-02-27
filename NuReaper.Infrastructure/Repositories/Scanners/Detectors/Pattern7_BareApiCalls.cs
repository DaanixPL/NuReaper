using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Extensions.Logging;
using NuReaper.Application.DTOs;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Patterns.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Detectors
{
    public class Pattern7_BareApiCalls : IPatternDetector
    {
        private readonly IPatternRegistry _patternRegistry;
        private readonly ICreateFinding _createFinding;
        private readonly IExtractApiCallArguments _extractApiCallArguments;
        private readonly ILogger<Pattern7_BareApiCalls> _logger;
        public Pattern7_BareApiCalls(IPatternRegistry patternRegistry, ICreateFinding createFinding, IExtractApiCallArguments extractApiCallArguments, ILogger<Pattern7_BareApiCalls> logger)
        {
            _createFinding = createFinding;
            _extractApiCallArguments = extractApiCallArguments;
            _patternRegistry = patternRegistry;
            _logger = logger;
        }

        public bool CanDetect(Instruction instruction)
        {
             if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                    instruction.Operand is IMethod bareApiMethod &&
                    _patternRegistry.IsHighRiskBareApiCall(bareApiMethod.FullName))
            {
                return true;
            }
            return false;
        }

        public List<FindingSummaryDto> Detect(IList<Instruction> instructions, int instructionIndex, TypeDef type, MethodDef method, HashSet<int> processedIndices)
        {
            var sb = new StringBuilder();
            List<FindingSummaryDto> findings = new List<FindingSummaryDto>();
            sb.AppendLine($"[Pattern7] Detected potential bare API call at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}");
            var bareApiMethod = instructions[instructionIndex].Operand as IMethod ?? throw new InvalidOperationException("Expected an IMethod operand for a call instruction.");
            var args = _extractApiCallArguments.Execute(instructions, instructionIndex, bareApiMethod);
            string evidence = $"API: {bareApiMethod.Name}, Args: {string.Join(", ", args)}";
            sb.AppendLine($"  --> Extracted API call arguments: {string.Join(", ", args)}");
            findings.Add(_createFinding.Execute(
                evidence,
                bareApiMethod.FullName,
                type,
                method,
                instructionIndex,
                hopDepth: 0,
                isLiteral: false,
                flowTrace: sb.ToString()
            ));
            sb.AppendLine($"     --> Created finding for bare API call \"{bareApiMethod.FullName}\" at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}");
            _logger.LogTrace(sb.ToString());
            processedIndices.Add(instructionIndex); // Mark the API call as processed
            return findings;
        }
    }
}
