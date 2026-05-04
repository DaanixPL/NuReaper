using NuReaper.Application.DTOs.DataFlow;

namespace NuReaper.Application.Interfaces.DataFlow
{
    public interface IDataFlowGraphBuilder
    {
        DataFlowGraphDto Build(
            IReadOnlyList<(string packageName, string dllPath)> inputs,
            CancellationToken cancellationToken = default);
    }
}