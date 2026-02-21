using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.FindingCreation
{
    public class GetVariableName : IGetVariableName
    {
        public string Execute(Instruction instr)
        {
            return instr.OpCode.Name switch
            {
                "stloc.0" => "var_0",
                "stloc.1" => "var_1",
                "stloc.2" => "var_2",
                "stloc.3" => "var_3",
                "stloc.s" => "var_s",
                "stloc" => "var_local",
                "stfld" => "field",
                "stsfld" => "static_field",
                _ => instr.OpCode.Name
            };
        }
    }
}
