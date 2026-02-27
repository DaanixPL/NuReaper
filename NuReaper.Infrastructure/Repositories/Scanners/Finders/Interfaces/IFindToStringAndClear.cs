using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces
{
    public interface IFindToStringAndClear
    {
        public int Execute(IList<Instruction> instructions, int startIndex, HashSet<int> processedIndices);
    }
}
