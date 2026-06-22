using NuReaper.Domain.Enums;

namespace NuReaper.Application.DTOs.DataFlow
{
    public class DataFlowNodeDto
    {
        public string Id { get; set; } = string.Empty;
        public DataFlowNodeType Type { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TypeFullName { get; set; } = string.Empty;
        public int PackageId { get; set; }
        public string AssemblyName { get; set; } = string.Empty;

        public string? ContainingMethodNodeId { get; set; }

        public uint? InstructionOffset { get; set; }
        public string? TargetMethodNodeId { get; set; }
    }
}