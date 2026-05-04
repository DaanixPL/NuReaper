using NuReaper.Domain.Enums;

namespace NuReaper.Domain.Entities.DataFlow
{
    public class DataFlowEdge
    {
        public string FromId { get; set; } = string.Empty;
        public string ToId { get; set; } = string.Empty;
        public DataFlowEdgeType EdgeType { get; set; }

        public int? ArgumentIndex { get; set; } 
    }
}