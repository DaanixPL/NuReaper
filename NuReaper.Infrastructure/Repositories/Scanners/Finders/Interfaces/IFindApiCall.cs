
using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces
{
    public interface IFindApiCall
    {
        public OpCode? Execute(IList<Instruction> instructions, int startIndex, List<OpCode> ApiCallsToFind);
    }
}
