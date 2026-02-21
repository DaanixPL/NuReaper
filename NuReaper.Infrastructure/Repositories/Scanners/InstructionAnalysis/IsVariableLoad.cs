using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis
{
    public class IsVariableLoad : IIsVariableLoad
    {
        public bool Execute(Instruction instr)
        {
            return instr.OpCode == OpCodes.Ldloc_0 ||
                    instr.OpCode == OpCodes.Ldloc_1 ||
                    instr.OpCode == OpCodes.Ldloc_2 ||
                    instr.OpCode == OpCodes.Ldloc_3 ||
                    instr.OpCode == OpCodes.Ldloc_S ||
                    instr.OpCode == OpCodes.Ldloc ||
                    instr.OpCode == OpCodes.Ldfld ||
                    instr.OpCode == OpCodes.Ldsfld ||
                    instr.OpCode == OpCodes.Ldarg_0 ||
                    instr.OpCode == OpCodes.Ldarg_1 ||
                    instr.OpCode == OpCodes.Ldarg_2 ||
                    instr.OpCode == OpCodes.Ldarg_3 ||
                    instr.OpCode == OpCodes.Ldarg_S ||
                    instr.OpCode == OpCodes.Ldarg;        
        }
    }
}
