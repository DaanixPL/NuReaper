using dnlib.DotNet;
using dnlib.DotNet.Emit;
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
        public Pattern7_BareApiCalls(IPatternRegistry patternRegistry, ICreateFinding createFinding, IExtractApiCallArguments extractApiCallArguments)
        {
            _createFinding = createFinding;
            _extractApiCallArguments = extractApiCallArguments;
            _patternRegistry = patternRegistry;
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
            List<FindingSummaryDto> findings = new List<FindingSummaryDto>();
            Console.WriteLine($"Detected potential bare API call at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}");
            var bareApiMethod = instructions[instructionIndex].Operand as IMethod ?? throw new InvalidOperationException("Expected an IMethod operand for a call instruction.");
            var args = _extractApiCallArguments.Execute(instructions, instructionIndex, bareApiMethod);
            string evidence = $"API: {bareApiMethod.Name}, Args: {string.Join(", ", args)}";
            Console.WriteLine($"  --> Extracted API call arguments: {string.Join(", ", args)}");
            findings.Add(_createFinding.Execute(
                evidence,
                bareApiMethod.FullName,
                type,
                method,
                instructionIndex,
                hopDepth: 0,
                isLiteral: false,
                flowTrace: new List<string> { $"High-risk API call: {bareApiMethod.Name}" }
            ));
            processedIndices.Add(instructionIndex); // Mark the API call as processed
            return findings;
        }
    }
}
