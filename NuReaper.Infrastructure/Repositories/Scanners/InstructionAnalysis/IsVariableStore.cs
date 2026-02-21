using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis
{
    public class IsVariableStore : IIsVariableStore
    {
        public bool Execute(Instruction instr)
        {
             return instr.OpCode == OpCodes.Stloc_0 ||
                   instr.OpCode == OpCodes.Stloc_1 ||
                   instr.OpCode == OpCodes.Stloc_2 ||
                   instr.OpCode == OpCodes.Stloc_3 ||
                   instr.OpCode == OpCodes.Stloc_S ||
                   instr.OpCode == OpCodes.Stloc ||
                   instr.OpCode == OpCodes.Stfld ||
                   instr.OpCode == OpCodes.Stsfld;
        }
    }
}
