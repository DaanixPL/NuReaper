using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces
{
    public interface IFindNextVariableStore
    {
        public (int Index, Instruction Instruction)? Execute(
            IList<Instruction> instructions,
            int startIndex,
            int window);
    }
}
