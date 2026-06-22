using NuReaper.Domain.Enums;

namespace NuReaper.Domain.Entities.DataFlow
{
    public readonly struct DataFlowEdge
    {
        public int Id { get; init; }
        public int FromId { get; init; }
        public int ToId { get; init; }
        public DataFlowEdgeType EdgeType { get; init; }
        public int ArgumentIndex { get; init; } // -1 = null
    }
}