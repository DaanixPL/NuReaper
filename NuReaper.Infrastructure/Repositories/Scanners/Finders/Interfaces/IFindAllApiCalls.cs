using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces
{
    public interface IFindAllApiCalls
    {
        public List<OpCode> Execute(IList<Instruction> instructions, int startIndex, List<OpCode> ApiCallsToFind);
    }
}