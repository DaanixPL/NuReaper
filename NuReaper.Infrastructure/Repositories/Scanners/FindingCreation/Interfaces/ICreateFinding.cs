using dnlib.DotNet;
using NuReaper.Application.DTOs;

namespace NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces
{
    public interface ICreateFinding
    {
        public FindingSummaryDto Execute(
            string evidence,
            string? apiCall,
            TypeDef type,
            MethodDef method,
            int instructionIndex,
            int hopDepth,
            bool isLiteral,
            List<string> flowTrace);
    }
}
