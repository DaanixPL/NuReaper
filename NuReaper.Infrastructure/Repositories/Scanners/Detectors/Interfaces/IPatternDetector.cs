using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Domain.Entities;

namespace NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces
{
    public interface IPatternDetector
    {
        bool CanDetect(Instruction instruction);

        List<ScanFinding> Detect(
            IList<Instruction> instructions,
            int instructionIndex,
            TypeDef type,
            MethodDef method,
            HashSet<int> processedIndices);
    }
}
