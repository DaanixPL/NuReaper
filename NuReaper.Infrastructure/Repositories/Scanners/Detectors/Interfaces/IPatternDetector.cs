using NuReaper.Domain.Entities;
using NuReaper.Domain.Entities.DataFlow;

namespace NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces
{
    public interface IPatternDetector
    {
        List<ScanFinding> Detect(
            DataFlowGraph graph,
            Dictionary<Package, int> packageIdToGuid,
            Dictionary<int, List<int>> incomingEdgesLookup,
            Guid scanId);
        
    }
}
