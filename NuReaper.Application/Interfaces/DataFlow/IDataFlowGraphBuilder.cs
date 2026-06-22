using NuReaper.Domain.Entities;
using NuReaper.Domain.Entities.DataFlow;

namespace NuReaper.Application.Interfaces.DataFlow
{
    public interface IDataFlowGraphBuilder
    {
        (DataFlowGraph, Dictionary<Package, int>) Build(
            IReadOnlyList<(string packageId, string dllPath)> inputs,
            List<Package> packages,
            CancellationToken cancellationToken = default);
    }
}