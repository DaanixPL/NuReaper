using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis.Interfaces
{
    public interface IVariableIndexExtractor
    {
        public int Execute(Instruction instr);
    }
}
