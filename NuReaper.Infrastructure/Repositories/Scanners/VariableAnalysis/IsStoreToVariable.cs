using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis
{
    public class IsStoreToVariable : IIsStoreToVariable
    {
        public bool Execute(Instruction instr, int varIndex)
        {
            bool result = instr.OpCode.Name switch
            {
                "stloc.0" => varIndex == 0,
                "stloc.1" => varIndex == 1,
                "stloc.2" => varIndex == 2,
                "stloc.3" => varIndex == 3,
                "stloc.s" => (instr.Operand as Local)?.Index == varIndex,
                "stloc" => (instr.Operand as Local)?.Index == varIndex,
                _ => false
            };
            return result;
        }
    }
}
