using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders
{
    public class FindApiCall : IFindApiCall
    {
        public OpCode? Execute(IList<Instruction> instructions, int startIndex, List<OpCode> ApiCallsToFind)
        {
            for (int i = startIndex + 1; i < instructions.Count - 1; i++)
            {
                var instr = instructions[i];
                if (instr == null) continue;

                if (ApiCallsToFind.Contains(instr.OpCode))
                {
                    return instr.OpCode;
                }
            }

            return null;
        }
    }
}
