using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis
{
    public class VariableIndexExtractor : IVariableIndexExtractor
    {
        public int Execute(Instruction instr)
        {
             return instr.OpCode.Name switch
            {
                "ldloc.0" => 0,
                "ldloc.1" => 1,
                "ldloc.2" => 2,
                "ldloc.3" => 3,
                "ldloc.s" => (instr.Operand as Local)?.Index ?? -1,
                "ldloc" => (instr.Operand as Local)?.Index ?? -1,
                _ => -1
            };
        }
    }
}
