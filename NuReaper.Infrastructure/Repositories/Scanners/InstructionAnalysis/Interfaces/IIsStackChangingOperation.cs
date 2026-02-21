using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces
{
    public interface IIsStackChangingOperation
    {
        public bool Execute(Instruction instr);
    }
}
