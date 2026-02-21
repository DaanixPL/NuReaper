using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces
{
    public interface IIsVariableStore
    {
        public bool Execute(Instruction instr);
    }
}
