using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.Extensions.Logging;
using NuReaper.Application.DTOs;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Patterns.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Analysis
{
    public class ScanMethod : IScanMethod
    {
        private readonly IPatternDetector[] _patternDetectors;
        private readonly ILogger<ScanMethod> _logger;

        public ScanMethod(IEnumerable<IPatternDetector> patternDetectors, ILogger<ScanMethod> logger)
        {
            _patternDetectors = patternDetectors.ToArray();
            _logger = logger;
        }

        public List<FindingSummaryDto> Execute(MethodDef method, TypeDef type)
        {
            var instructions = method.Body.Instructions;

            if (instructions.Count == 0)
                return new List<FindingSummaryDto>();
            
            var findings = new List<FindingSummaryDto>(capacity: 4);
            var processedIndices = new HashSet<int>(); // Deduplication

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                 var sb = new StringBuilder();
                sb.AppendLine("=== FULL IL DUMP ===");
                for (int idx = 0; idx < instructions.Count; idx++)
                {
                    var inst = instructions[idx];
                    string operandStr = inst.Operand  switch
                    {
                        string s => $"\"{s}\"",
                        IMethod m => $"{m.DeclaringType?.Name}::{m.Name}",
                        ITypeDefOrRef t => t.Name,
                        _ => inst.Operand?.ToString() ?? ""
                    };
                    sb.AppendLine($"  IL_{idx:X4}: {inst.OpCode.Name,-15} {operandStr}");
                }
                sb.AppendLine("=== END IL DUMP ===");
                _logger.LogTrace(sb.ToString());
            }
           

            for (int i = 0; i < instructions.Count; i++)
            {
                if (processedIndices.Contains(i))
                    continue; 

                var instr = instructions[i];
  
                for (int d = 0; d < _patternDetectors.Length; d++)
                {
                    var patternDetector = _patternDetectors[d];
                    if (patternDetector.CanDetect(instr))
                    {
                        var patternFindings = patternDetector.Detect(instructions, i, type, method, processedIndices);
                        if (patternFindings.Count > 0)
                        {
                            findings.AddRange(patternFindings);
                            break;
                        }
                    }
                }
            }
            return findings;
        }
    }
}
