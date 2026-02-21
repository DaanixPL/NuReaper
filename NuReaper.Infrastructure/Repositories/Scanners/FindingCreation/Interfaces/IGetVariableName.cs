using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces
{
    public interface IGetVariableName
    {
        public string Execute(Instruction instr);
    }
}
