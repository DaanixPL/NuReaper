using dnlib.DotNet;
using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Infrastructure.Repositories.DataFlow.Registries;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Analysis.Interfaces
{
    public interface IAnalyzeMethod
    {
        public void Execute(
            MethodDef method,
            TypeDef type,
            string packageKey,
            int packageId,
            ModuleDef module,
            MultiPackageResolver resolver,
            NodeRegistry nodes,
            List<DataFlowEdge> edges);
    }
}
