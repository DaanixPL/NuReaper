using dnlib.DotNet.Emit;

namespace NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces
{
    public interface IFindApiCallUsingVariable
    {
        public string? Execute(IList<Instruction> instructions, int startIndex, int window);
    }
}
