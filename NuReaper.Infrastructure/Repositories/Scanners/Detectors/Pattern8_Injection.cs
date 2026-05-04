using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Domain.Entities;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Patterns.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Detectors
{
    public class Pattern8_Injection : IPatternDetector
    {
        private readonly IPatternRegistry _patternRegistry;
        private readonly IFindApiCall _findApiCall;

        public Pattern8_Injection(IPatternRegistry patternRegistry, IFindApiCall findApiCall)
        {
            _patternRegistry = patternRegistry;
            _findApiCall = findApiCall;
        }   

        public bool CanDetect(Instruction instruction)
        {
            return _patternRegistry.IsInjectionSink(instruction.Operand?.ToString() ?? string.Empty);
        }

        public List<ScanFinding> Detect(IList<Instruction> instructions, int instructionIndex, TypeDef type, MethodDef method, HashSet<int> processedIndices)
        {
            List<ScanFinding> findings = new List<ScanFinding>();
            var injectionSink = instructions[instructionIndex].Operand?.ToString();
            var category = _patternRegistry.FindCategory(injectionSink ?? string.Empty);
            _findApiCall.Execute(instructions, instructionIndex, new List<OpCode> { OpCodes.Call, OpCodes.Callvirt, OpCodes.Calli, OpCodes.Newobj });
            

            return findings;
        }
    }
}