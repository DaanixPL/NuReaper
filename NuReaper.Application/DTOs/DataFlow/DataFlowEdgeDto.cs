using NuReaper.Domain.Enums;

namespace NuReaper.Application.DTOs.DataFlow
{
    public class DataFlowEdgeDto
    {
        public string FromId { get; set; } = string.Empty;
        public string ToId { get; set; } = string.Empty;
        public DataFlowEdgeType EdgeType { get; set; }

        public int? ArgumentIndex { get; set; }
    }
}