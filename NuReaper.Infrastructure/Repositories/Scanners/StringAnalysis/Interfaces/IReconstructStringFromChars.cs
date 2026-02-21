using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis.Interfaces
{
    public interface IReconstructStringFromChars
    {
        public string Execute(IList<Instruction> instructions, int startIndex);
    }
}
