using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Application.DTOs;

namespace NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces
{
    public interface IPatternDetector
    {
        bool CanDetect(Instruction instruction);

        List<FindingSummaryDto> Detect(
            IList<Instruction> instructions,
            int instructionIndex,
            TypeDef type,
            MethodDef method,
            HashSet<int> processedIndices);
    }
}
