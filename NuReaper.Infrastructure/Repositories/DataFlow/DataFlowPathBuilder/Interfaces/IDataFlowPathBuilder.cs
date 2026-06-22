using NuReaper.Domain.Entities.DataFlow;

namespace NuReaper.Infrastructure.Repositories.DataFlow.DataFlowPathBuilder.Interfaces
{
    public interface IDataFlowPathBuilder
    {
        public void BuildPaths(DataFlowGraph graph, Action<DataFlowPath> onPathFound, CancellationToken cancellationToken);
    }
}
