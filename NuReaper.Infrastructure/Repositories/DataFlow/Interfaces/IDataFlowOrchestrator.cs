using NuReaper.Domain.Entities;
using NuReaper.Domain.Entities.DataFlow;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Interfaces
{
    public interface IDataFlowOrchestrator
    {
           Task<(DataFlowGraph, Dictionary<Package, int>)> BuildAsync(
            string rootPackageName,
            string rootPackageVersion,
            CancellationToken cancellationToken = default);

           Task<(DataFlowGraph, Dictionary<Package, int>)> BuildLocalNupkgAsync(
            string nupkgPath,
            string rootPackageName,
            string rootPackageVersion,
            CancellationToken cancellationToken = default);
    }
}