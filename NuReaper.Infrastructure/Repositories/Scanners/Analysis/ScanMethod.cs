using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Application.DTOs;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Patterns.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Analysis
{
    public class ScanMethod : IScanMethod
    {
        private readonly IEnumerable<IPatternDetector> _patternDetectors;

        public ScanMethod(IPatternRegistry patternRegistry, IEnumerable<IPatternDetector> patternDetectors)
        {
            _patternDetectors = patternDetectors;
        }

        public List<FindingSummaryDto> Execute(MethodDef method, TypeDef type)
        {
            var findings = new List<FindingSummaryDto>();
            var instructions = method.Body.Instructions;
            var processedIndices = new HashSet<int>(); // Deduplication

                Console.WriteLine("\n📋 FULL IL DUMP:");
                for (int idx = 0; idx < instructions.Count; idx++)
                {
                    var inst = instructions[idx];
                    string operandStr = inst.Operand switch
                    {
                        string s => $"\"{s}\"",
                        IMethod m => $"{m.DeclaringType?.Name}::{m.Name}",  // ✅ Pokaż pełną nazwę
                        ITypeDefOrRef t => t.Name,
                        _ => inst.Operand?.ToString() ?? ""
                    };
                    Console.WriteLine($"  IL_{idx:X4}: {inst.OpCode.Name,-15} {operandStr}");
                }
                Console.WriteLine("📋 END IL DUMP\n");

            for (int i = 0; i < instructions.Count; i++)
            {
                var instr = instructions[i];

                if (processedIndices.Contains(i))
                    continue; 
                foreach (var patternDetector in _patternDetectors)
                {
                    if (patternDetector.CanDetect(instr))
                    {
                        // Console.WriteLine($"Found string: \"{instr.Operand}\" at IL_{instr.Offset:X4} in {type.FullName}::{method.Name}");
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
