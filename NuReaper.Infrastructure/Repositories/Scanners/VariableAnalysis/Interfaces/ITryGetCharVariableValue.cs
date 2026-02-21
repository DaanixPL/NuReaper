using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis.Interfaces
{
    public interface ITryGetCharVariableValue
    {
        public char? Execute(IList<Instruction> instructions, int loadIndex);
    }
}
