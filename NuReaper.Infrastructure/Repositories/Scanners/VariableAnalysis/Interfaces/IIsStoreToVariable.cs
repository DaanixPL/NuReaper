using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis.Interfaces
{
    public interface IIsStoreToVariable
    {
        public bool Execute(Instruction instr, int varIndex);
    }
}
