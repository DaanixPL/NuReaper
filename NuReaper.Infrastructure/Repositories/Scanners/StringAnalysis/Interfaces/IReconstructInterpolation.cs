using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis.Interfaces
{
    public interface IReconstructInterpolation
    {
        public (bool IsInterpolated, string ReconstructedString, int ConcatIndex, List<int> ProcessedIndices) 
        Execute(IList<Instruction> instructions, int startIndex);
    }
}
