using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders
{
    public class FindToStringAndClear : IFindToStringAndClear
    {
        public int Execute(IList<Instruction> instructions, int startIndex)
        {
            const int maxWindow = 100;
            
            for (int i = startIndex; i < Math.Min(startIndex + maxWindow, instructions.Count); i++)
            {
                var instr = instructions[i];
                
                if ((instr.OpCode == OpCodes.Call || instr.OpCode == OpCodes.Callvirt) &&
                    instr.Operand is IMethod method &&
                    method.Name == "ToStringAndClear")
                {
                    return i;
                }
            }
            
            return -1;
        }
    }
}
