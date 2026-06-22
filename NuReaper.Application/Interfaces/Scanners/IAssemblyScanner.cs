using NuReaper.Domain.Entities;
using NuReaper.Domain.Entities.DataFlow;

namespace NuReaper.Application.Interfaces.Scanners
{
    public interface IAssemblyScanner
    {
        Task<ScanPackageResult> ScanPackageAsync(
            string rootPackageName,
            string rootPackageVersion,
            DataFlowGraph graph,
            Dictionary<Package, int> packageIdToGuid,
            Guid jobId,
            CancellationToken cancellationToken);
    }
}