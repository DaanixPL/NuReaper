using NuReaper.Application.DTOs.DataFlow;

namespace NuReaper.Infrastructure.Repositories.DataFlow
{
    internal sealed class StackSlot
    {
        public DataFlowNodeDto? VarNode { get; init; }
        public DataFlowNodeDto? CallResultOf { get; init; }
        public DataFlowNodeDto? FieldNode { get; init; }
        public DataFlowNodeDto? LiteralNode { get; init; }
        public DataFlowNodeDto? ArrayNode { get; init; }

        public DataFlowNodeDto? AnyNode =>
            VarNode ?? CallResultOf ?? FieldNode ?? LiteralNode ?? ArrayNode;

        public static readonly StackSlot Unknown = new();
    }
}