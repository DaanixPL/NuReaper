using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis
{
    public class IsStackChangingOperation : IIsStackChangingOperation
    {
        public bool Execute(Instruction instr)
        {
              return instr.OpCode == OpCodes.Call ||
                   instr.OpCode == OpCodes.Callvirt ||
                   instr.OpCode == OpCodes.Newobj ||
                   instr.OpCode == OpCodes.Ret ||
                   instr.OpCode == OpCodes.Br ||
                   instr.OpCode == OpCodes.Br_S;
        }
    }
}
