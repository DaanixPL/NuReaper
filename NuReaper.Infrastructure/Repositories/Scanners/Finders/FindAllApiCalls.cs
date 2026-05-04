using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders
{
    public class FindAllApiCalls : IFindAllApiCalls
    {
        public List<OpCode> Execute(IList<Instruction> instructions, int startIndex, List<OpCode> ApiCallsToFind)
        {
            List<OpCode> foundApiCalls = new List<OpCode>();
            for (int i = startIndex + 1; i < instructions.Count - 1; i++)
            {
                var instr = instructions[i];
                if (instr == null) continue;

                if (ApiCallsToFind.Contains(instr.OpCode))
                {
                    foundApiCalls.Add(instr.OpCode);
                }
            }

            return foundApiCalls;
        }
    }
}