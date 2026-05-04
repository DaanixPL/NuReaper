using dnlib.DotNet;
using NuReaper.Domain.Entities;

namespace NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces
{
    public interface ICreateFinding
    {
        public ScanFinding Execute(
            string evidence,
            string? apiCall,
            TypeDef type,
            MethodDef method,
            int instructionIndex,
            int hopDepth,
            bool isLiteral,
            string flowTrace);
    }
}
