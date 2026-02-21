using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis.Interfaces
{
    public interface ICollectStackArguments
    {
        public List<string> Execute(IList<Instruction> instructions, int callIndex, int paramCount);
    }
}
