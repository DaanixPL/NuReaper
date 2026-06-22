using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Domain.Enums;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Helpers
{
    public static class EdgeFactory
    {
        public static DataFlowEdge Edge(int from, int to, DataFlowEdgeType type)
            => new() { FromId = from, ToId = to, EdgeType = type };
    }
}
