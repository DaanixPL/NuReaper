using NuReaper.Domain.Entities;
using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Detectors
{
    public class Pattern2 : IPatternDetector
    {
        public List<ScanFinding> Detect(DataFlowGraph graph, Dictionary<Package, int> packageIdToGuid, Dictionary<int, List<int>> incomingEdgesLookup, Guid scanId)
        {
            return new List<ScanFinding>();
        }
    }
}
