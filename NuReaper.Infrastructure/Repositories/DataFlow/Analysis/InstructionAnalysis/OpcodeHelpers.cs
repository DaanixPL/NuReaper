using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.DataFlow.Analysis.InstructionAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Analysis.InstructionAnalysis
{
    public class OpcodeHelpers : IOpcodeHelpers
    {
        public bool TryGetArgSigIndex(Instruction instr, MethodDef method, out int sigIndex)
        {
            int rawParamIdx;
            switch (instr.OpCode.Code)
            {
                case Code.Ldarg_0: rawParamIdx = 0; break;
                case Code.Ldarg_1: rawParamIdx = 1; break;
                case Code.Ldarg_2: rawParamIdx = 2; break;
                case Code.Ldarg_3: rawParamIdx = 3; break;
                case Code.Ldarg_S:
                case Code.Ldarg:
                    if (instr.Operand is Parameter p)
                    {
                        sigIndex = p.MethodSigIndex; 
                        return sigIndex >= 0;
                    }
                    sigIndex = -1;
                    return false;
                default:
                    sigIndex = -1;
                    return false;
            }

            if (rawParamIdx < method.Parameters.Count)
            {
                sigIndex = method.Parameters[rawParamIdx].MethodSigIndex;
                return sigIndex >= 0; 
            }

            sigIndex = -1;
            return false;
        }

        public bool TryGetLocalIndex(Instruction instr, out int index)
        {
             switch (instr.OpCode.Code)
            {
                case Code.Ldloc_0: index = 0; return true;
                case Code.Ldloc_1: index = 1; return true;
                case Code.Ldloc_2: index = 2; return true;
                case Code.Ldloc_3: index = 3; return true;
                case Code.Ldloc_S:
                case Code.Ldloc:
                    if (instr.Operand is Local loc) { index = loc.Index; return true; }
                    break;
            }
            index = -1;
            return false;
        }

        public bool TryGetStoreLocalIndex(Instruction instr, out int index)
        {
            switch (instr.OpCode.Code)
            {
                case Code.Stloc_0: index = 0; return true;
                case Code.Stloc_1: index = 1; return true;
                case Code.Stloc_2: index = 2; return true;
                case Code.Stloc_3: index = 3; return true;
                case Code.Stloc_S:
                case Code.Stloc:
                    if (instr.Operand is Local loc) { index = loc.Index; return true; }
                    break;
            }
            index = -1;
            return false;
        }
    }
}
