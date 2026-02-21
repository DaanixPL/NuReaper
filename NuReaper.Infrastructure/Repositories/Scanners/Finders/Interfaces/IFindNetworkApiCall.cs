
using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces
{
    public interface IFindNetworkApiCall
    {
        public string? Execute(IList<Instruction> instructions, int startIndex);
    }
}
