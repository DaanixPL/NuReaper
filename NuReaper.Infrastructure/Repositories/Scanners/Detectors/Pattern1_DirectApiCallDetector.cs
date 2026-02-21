using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Application.DTOs;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Patterns.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Detectors
{
    public class Pattern1_DirectApiCallDetector : IPatternDetector
    {
        private readonly IFindNetworkApiCall _findNetworkApiCall;
        private readonly ICreateFinding _createFinding;
        private readonly IPatternRegistry _patternRegistry;

        public Pattern1_DirectApiCallDetector(IFindNetworkApiCall findNetworkApiCall, ICreateFinding createFinding, IPatternRegistry patternRegistry)
        {
            _findNetworkApiCall = findNetworkApiCall;
            _createFinding = createFinding;
            _patternRegistry = patternRegistry;
        }




        public bool CanDetect(Instruction instruction)
        {
            Console.WriteLine($"Pattern1\n   --> is a string load with value: \"{instruction.OpCode}\"");
            if (instruction.OpCode == OpCodes.Ldstr)
            {
                Console.WriteLine("     --> CAN BE DETECTED");
                return true;
            }
            Console.WriteLine("     --> CAN'T BE DETECTED");
            return false;
        }

        public List<FindingSummaryDto> Detect(IList<Instruction> instructions, int instructionIndex, TypeDef type, MethodDef method,
                 HashSet<int> processedIndices)
        {
            // Pattern 1: Direct usage - string immediately used in API call

            List<FindingSummaryDto> findings = new List<FindingSummaryDto>();
            var stringValue = (string)instructions[instructionIndex].Operand;
            Console.WriteLine($"Pattern1_DirectApiCallDetector: Checking for direct API calls using string \"{stringValue}\" at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}");

                var suspiciousString = _patternRegistry.IsSuspiciousString(stringValue);
                Console.WriteLine("-------------------------   PATTERN 1   --------------------------------");
                Console.WriteLine($"String value: {stringValue} | Suspicious: {suspiciousString}");
                Console.WriteLine("------------------------------------------------------------------------");
                if (suspiciousString == ScanFindingType.None)  
                    return findings; // Skip if string is not suspicious
                Console.WriteLine($"   --> Pattern1_DirectApiCallDetector: Analyzing instruction at IL_{instructions[instructionIndex].Offset:X4}: {instructions[instructionIndex]}");
                var directApiCall = _findNetworkApiCall.Execute(instructions, instructionIndex);
                if (!string.IsNullOrEmpty(directApiCall))
                {
                    findings.Add(_createFinding.Execute(
                        stringValue,
                        directApiCall,
                        type,
                        method,
                        instructionIndex,
                        hopDepth: 0,
                        isLiteral: true,
                        flowTrace: new List<string> { $"direct usage ( {suspiciousString} ) in API call Pattern 1" }
                    ));
                    processedIndices.Add(instructionIndex); // Mark as processed
                }
            Console.WriteLine($"Pattern1_DirectApiCallDetector: No direct API call found for string \"{stringValue}\" at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}");
            return findings;
        }
    }
}
