using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Extensions.Logging;
using NuReaper.Domain.Entities;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.Scanners.ApiCallRegistry.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Patterns.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Detectors
{
    public class Pattern1_DirectApiCallDetector : IPatternDetector
    {
        private readonly IFindApiCall _findApiCall;
        private readonly IIsNetworkApiCall _isNetworkApiCall;
        private readonly ICreateFinding _createFinding;
        private readonly IPatternRegistry _patternRegistry;
        private readonly ILogger<Pattern1_DirectApiCallDetector> _logger;
        public Pattern1_DirectApiCallDetector(IFindApiCall findApiCall, IIsNetworkApiCall isNetworkApiCall, ICreateFinding createFinding, IPatternRegistry patternRegistry, ILogger<Pattern1_DirectApiCallDetector> logger)
        {
            _findApiCall = findApiCall;
            _isNetworkApiCall = isNetworkApiCall;
            _createFinding = createFinding;
            _patternRegistry = patternRegistry;
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

        public List<ScanFinding> Detect(IList<Instruction> instructions, int instructionIndex, TypeDef type, MethodDef method,
                 HashSet<int> processedIndices)
        {
            // Pattern 1: Direct usage - string immediately used in API call
            var sb = new StringBuilder();
            List<ScanFinding> findings = new List<ScanFinding>();
            var stringValue = (string)instructions[instructionIndex].Operand;
            sb.AppendLine($"[Pattern1] Checking for direct API calls using string \"{stringValue}\" at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}");

                var suspiciousString = _patternRegistry.IsSuspiciousString(stringValue);
                sb.AppendLine($"   --> String value: {stringValue} | Suspicious: {suspiciousString}");
                if (suspiciousString == ScanFindingType.None)
                {
                    _logger.LogTrace(sb.ToString());
                    return findings; // Skip if string is not suspicious
                }
                sb.AppendLine($"      --> Pattern1_DirectApiCallDetector: Analyzing instruction at IL_{instructions[instructionIndex].Offset:X4}: {instructions[instructionIndex]}");
                var directApiCall = _findApiCall.Execute(instructions, instructionIndex, new List<OpCode> { OpCodes.Call, OpCodes.Callvirt, OpCodes.Newobj })?.ToString();
                bool isDirectApiCall = _isNetworkApiCall.Execute(directApiCall ?? string.Empty);
                if (isDirectApiCall)
                {
                    sb.AppendLine($"         --> Found direct API call \"{directApiCall}\" using string \"{stringValue}\"");

                    findings.Add(_createFinding.Execute(
                        stringValue,
                        directApiCall,
                        type,
                        method,
                        instructionIndex,
                        hopDepth: 0,
                        isLiteral: true,
                        flowTrace: sb.ToString()
                    ));
                    processedIndices.Add(instructionIndex); // Mark as processed
                }
                else 
                {
                    sb.AppendLine($"         --> No direct API call found for string \"{stringValue}\" at IL_{instructions[instructionIndex].Offset:X4} in {type.FullName}::{method.Name}");
                }
            _logger.LogTrace(sb.ToString());
            return findings;
        }
    }
}
