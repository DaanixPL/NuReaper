using dnlib.DotNet;
using NuReaper.Domain.Entities;

namespace NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces
{
    public interface IScanMethod
    {
        public List<ScanFinding> Execute(MethodDef method, TypeDef type);
    }
}
