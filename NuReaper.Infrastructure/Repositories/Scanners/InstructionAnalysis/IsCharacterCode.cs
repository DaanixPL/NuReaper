using NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis
{
    public class IsCharacterCode : IIsCharacterCode
     {
        public bool Execute(object? operand)
        {
        if (operand is int code)
            return code >= 32 && code <= 126;

        return false;
        }
    }
}
