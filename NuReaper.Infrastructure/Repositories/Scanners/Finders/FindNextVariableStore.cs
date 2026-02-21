using dnlib.DotNet.Emit;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders
{
    public class FindNextVariableStore : IFindNextVariableStore
    {
         private readonly IIsVariableStore _isVariableStore;

        public FindNextVariableStore(IIsVariableStore isVariableStore)
        {
            _isVariableStore = isVariableStore;
        }
        public (int Index, Instruction Instruction)? Execute(IList<Instruction> instructions, int startIndex, int window)
        {
            for (int i = startIndex + 1; i <= Math.Min(startIndex + window, instructions.Count - 1); i++)
            {
                if (_isVariableStore.Execute(instructions[i]))
                    return (i, instructions[i]);
            }

            return null;
        }
    }
}
