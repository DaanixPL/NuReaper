using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis.Interfaces
{
    public interface ITryGetVariableValue
    {
        public string? Execute(IList<Instruction> instructions, int loadIndex);
    }
}
