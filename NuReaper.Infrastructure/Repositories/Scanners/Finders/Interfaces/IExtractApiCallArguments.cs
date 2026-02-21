using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces
{
    public interface IExtractApiCallArguments
    {
        public List<string> Execute(IList<Instruction> instructions, int callIndex, IMethod method);
    }
}
