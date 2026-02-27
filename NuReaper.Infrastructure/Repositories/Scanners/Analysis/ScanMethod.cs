using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Extensions.Logging;
using NuReaper.Application.DTOs;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Patterns.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Analysis
{
    public class ScanMethod : IScanMethod
    {
        private readonly IEnumerable<IPatternDetector> _patternDetectors;
        private readonly ILogger<ScanMethod> _logger;

        public ScanMethod(IPatternRegistry patternRegistry, IEnumerable<IPatternDetector> patternDetectors, ILogger<ScanMethod> logger)
        {
            _patternDetectors = patternDetectors;
            _logger = logger;
        }

        public List<FindingSummaryDto> Execute(MethodDef method, TypeDef type)
        {
            var findings = new List<FindingSummaryDto>();
            var instructions = method.Body.Instructions;
            var processedIndices = new HashSet<int>(); // Deduplication

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

            for (int i = 0; i < instructions.Count; i++)
            {
                var instr = instructions[i];

                if (processedIndices.Contains(i))
                    continue; 
                foreach (var patternDetector in _patternDetectors)
                {
                    if (patternDetector.CanDetect(instr))
                    {
                        var patternFindings = patternDetector.Detect(instructions, i, type, method, processedIndices);
                        findings.AddRange(patternFindings);
                        if (patternFindings.Count > 0)
                            break;
                    }
                }
            }
            return findings;
        }
    }
}
