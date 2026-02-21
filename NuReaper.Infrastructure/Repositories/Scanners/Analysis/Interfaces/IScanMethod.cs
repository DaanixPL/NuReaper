using dnlib.DotNet;
using NuReaper.Application.DTOs;

namespace NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces
{
    public interface IScanMethod
    {
        public List<FindingSummaryDto> Execute(MethodDef method, TypeDef type);
    }
}
