using NuReaper.Application.DTOs.DataFlow;

namespace NuReaper.Infrastructure.Repositories.DataFlow
{
    internal sealed class StackSlot
    {
        public int VarNodeId      { get; init; }
        public int CallResultOfId { get; init; }
        public int FieldNodeId    { get; init; }
        public int LiteralNodeId  { get; init; }
        public int ArrayNodeId    { get; init; }

        public int AnyNodeId =>
                VarNodeId      != 0 ? VarNodeId      :
                CallResultOfId != 0 ? CallResultOfId :
                FieldNodeId    != 0 ? FieldNodeId    :
                LiteralNodeId  != 0 ? LiteralNodeId  :
                ArrayNodeId    != 0 ? ArrayNodeId    : 0;

        public static readonly StackSlot Unknown = new();
    }
}