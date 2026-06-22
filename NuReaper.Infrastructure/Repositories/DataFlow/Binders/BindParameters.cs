using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.DataFlow.Binders.Interface;
using NuReaper.Infrastructure.Repositories.DataFlow.Helpers;
using NuReaper.Infrastructure.Repositories.DataFlow.Registries;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Binders
{
    public class BindParameters : IBindParameters
    {
        public void Execute(NodeRegistry registry, List<DataFlowEdge> edges)
        {
       // ── 1. callSiteId → targetMethodId ───────────────────────────────────
            // Szukamy tylko Targets edges — jeden forward pass
            var callSiteToTarget = new Dictionary<int, int>();

            int originalEdgeCount = edges.Count;

            for (int i = 0; i < originalEdgeCount; i++)
            {
                ref readonly var e = ref System.Runtime.InteropServices.CollectionsMarshal
                    .AsSpan(edges)[i];

                if (e.EdgeType == DataFlowEdgeType.Targets)
                    callSiteToTarget.TryAdd(e.FromId, e.ToId);
            }

            // ── 2. methodId → { argIndex → nodeId } ──────────────────────────────
            var methodToParams = new Dictionary<int, Dictionary<int, int>>();

            foreach (var node in registry.Nodes)
            {
                if (node.Type != DataFlowNodeType.Variable)
                    continue;

                if (node.ContainingMethodNodeId == 0)
                    continue;

                if (string.IsNullOrEmpty(node.Name)
                    || !node.Name.StartsWith("arg_", StringComparison.Ordinal))
                    continue;

                var argIndex = ParseArgIndex.Execute(node.Name);
                if (argIndex < 0)
                    continue;

                if (!methodToParams.TryGetValue(node.ContainingMethodNodeId, out var paramMap))
                {
                    paramMap = new Dictionary<int, int>();
                    methodToParams[node.ContainingMethodNodeId] = paramMap;
                }

                paramMap.TryAdd(argIndex, node.Id);
            }

            // ── 3. FlowInto → ParameterBinding ───────────────────────────────────
            // Iterujemy tylko po oryginalnych (originalEdgeCount), nowe dodajemy na koniec.
            // Deduplikacja przez HashSet — unikamy powielonych ParameterBinding.
            var addedBindings = new HashSet<(int callSiteId, int paramNodeId, int argIndex)>();

            for (int i = 0; i < originalEdgeCount; i++)
            {
                ref readonly var flow = ref System.Runtime.InteropServices.CollectionsMarshal
                    .AsSpan(edges)[i];

                if (flow.EdgeType != DataFlowEdgeType.FlowInto || flow.ArgumentIndex < 0)
                    continue;

                if (!callSiteToTarget.TryGetValue(flow.ToId, out var targetMethodId))
                    continue;

                if (!methodToParams.TryGetValue(targetMethodId, out var paramMap))
                    continue;

                if (!paramMap.TryGetValue(flow.ArgumentIndex, out var paramNodeId))
                    continue;

                var key = (flow.ToId, paramNodeId, flow.ArgumentIndex);
                if (!addedBindings.Add(key))
                    continue;  

                edges.Add(new DataFlowEdge
                {
                    FromId        = flow.ToId,
                    ToId          = paramNodeId,
                    EdgeType      = DataFlowEdgeType.ParameterBinding,
                    ArgumentIndex = flow.ArgumentIndex
                });
            }
        }
    }
}
