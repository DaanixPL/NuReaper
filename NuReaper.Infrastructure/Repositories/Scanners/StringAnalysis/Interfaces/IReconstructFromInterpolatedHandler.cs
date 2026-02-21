using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis.Interfaces
{
    public interface IReconstructFromInterpolatedHandler
    {
        public string Execute(IList<Instruction> instructions, int newObjIndex);
    }
}
