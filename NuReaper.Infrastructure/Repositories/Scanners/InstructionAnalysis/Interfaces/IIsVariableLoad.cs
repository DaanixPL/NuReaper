using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces
{
    public interface IIsVariableLoad
    {
        public bool Execute(Instruction instr);
    }
}
