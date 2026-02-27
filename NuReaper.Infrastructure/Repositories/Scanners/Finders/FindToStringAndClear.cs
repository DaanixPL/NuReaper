using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Extensions.Logging;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders
{

    public class FindToStringAndClear : IFindToStringAndClear
    {
        private readonly ILogger<FindToStringAndClear> _logger;

        public FindToStringAndClear(ILogger<FindToStringAndClear> logger)
        {
            _logger = logger;
        }

        public int Execute(IList<Instruction> instructions, int startIndex, HashSet<int> processedIndices)
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
                if (instr.OpCode == OpCodes.Ldstr)
                {
                    processedIndices.Add(i);
                    _logger.LogTrace("     --> Marked IL_{Index:X4} (Ldstr \"{Operand}\") as processed by Pattern6", i, instr.Operand);
                }
            }
            
            return -1;
        }
    }
}
