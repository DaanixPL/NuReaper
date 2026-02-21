using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces
{
    public interface IExtractVariableIndex
    {
        public int Execute(Instruction instr);
    }
}
