using NuReaper.Domain.Entities.DataFlow;

namespace NuReaper.Infrastructure.Repositories.DataFlow.DataFlowPathBuilder.DFS.Interfaces
{
    public interface IDFS
    {
        public void DepthFirstSearch(
            DataFlowNode current,
            List<int> currentNodes,
            List<int> currentEdges,
            HashSet<int> visited,
            Dictionary<int, List<DataFlowEdge>> adjacency,
            Dictionary<int, DataFlowNode> nodeLookup,
            Action<DataFlowPath> onPathFound,
            HashSet<int> canReach,
            HashSet<int> methodsInCurrentPath,
            CancellationToken cancellationToken = default);
    }
}
