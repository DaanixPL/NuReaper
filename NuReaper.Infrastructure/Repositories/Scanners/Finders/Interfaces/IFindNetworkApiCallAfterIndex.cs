using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces
{
    public interface IFindNetworkApiCallAfterIndex
    {
        public string? Execute(IList<Instruction> instructions, int startIndex, int window = 50);
    }
}