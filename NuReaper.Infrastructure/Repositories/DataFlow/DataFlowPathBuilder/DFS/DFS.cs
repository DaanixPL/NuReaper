using Microsoft.Extensions.Logging;
using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.DataFlow.DataFlowPathBuilder.DFS.Interfaces;

namespace NuReaper.Infrastructure.Repositories.DataFlow.DataFlowPathBuilder.DFS
{
    public class DFS : IDFS
    {
        private readonly ILogger<DFS> _logger;
        public DFS(ILogger<DFS> logger)
        {
            _logger = logger;
        }
        private static readonly HashSet<DataFlowEdgeType> StructuralEdges = new()
        {
            DataFlowEdgeType.Calls,
            DataFlowEdgeType.Targets,
            DataFlowEdgeType.Contains
        };
        public void DepthFirstSearch(DataFlowNode current, List<int> currentNodes, List<int> currentEdges, HashSet<int> visited, Dictionary<int, List<DataFlowEdge>> adjacency, Dictionary<int, DataFlowNode> nodeLookup, Action<DataFlowPath> onPathFound, HashSet<int> canReach, HashSet<int> methodsInCurrentPath, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            if (current.Type == DataFlowNodeType.CallSite)
            {
                onPathFound(new DataFlowPath
                {
                    NodesIds = [.. currentNodes],
                    EdgesIds = [.. currentEdges]
                });

                bool hasReturnFlow = 
                    adjacency.TryGetValue(current.Id, out var callSiteEdges) &&
                    callSiteEdges.Any(e => e.EdgeType == DataFlowEdgeType.Returns);

                if (!hasReturnFlow)
                    return;
            }

            if (!visited.Add(current.Id))
                return;

            if (!adjacency.TryGetValue(current.Id, out var outEdges) || outEdges.Count == 0)
            {
                visited.Remove(current.Id);
                return;
            }

            foreach (var edge in outEdges)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;
                if (StructuralEdges.Contains(edge.EdgeType))
                    continue;
                
                if (!nodeLookup.TryGetValue(edge.ToId, out var next))
                    continue;
                
                if (!canReach.Contains(next.Id))
                    continue;
                
                if (edge.EdgeType == DataFlowEdgeType.ParameterBinding)
                {
                    var targetMethodId = next.ContainingMethodNodeId;
                    if (targetMethodId == 0 || !methodsInCurrentPath.Add(targetMethodId))
                        continue;

                    currentNodes.Add(next.Id);
                    currentEdges.Add(edge.Id);

                    DepthFirstSearch(next, currentNodes, currentEdges, visited, adjacency, nodeLookup, onPathFound, canReach, methodsInCurrentPath, cancellationToken);

                    currentNodes.RemoveAt(currentNodes.Count - 1);
                    currentEdges.RemoveAt(currentEdges.Count - 1);

                    methodsInCurrentPath.Remove(targetMethodId); // backtrack
                    continue;
                }

                currentNodes.Add(next.Id);
                currentEdges.Add(edge.Id);
                DepthFirstSearch(next, currentNodes, currentEdges, visited, adjacency, nodeLookup, onPathFound, canReach, methodsInCurrentPath, cancellationToken);

                currentNodes.RemoveAt(currentNodes.Count - 1);
                currentEdges.RemoveAt(currentEdges.Count - 1);
            }
            visited.Remove(current.Id);
        }
    }
}
