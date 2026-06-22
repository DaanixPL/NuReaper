using NuReaper.Domain.Enums;

namespace NuReaper.Domain.Entities.DataFlow
{
    public sealed class DataFlowNode
    {
        /// <summary>
        /// Unique identifier for the node
        /// Format:
        ///   Method:      "method:{pkg}:{methodFullName}"
        ///   Variable:    "var:{containingMethodNodeId}:{local_N|arg_N}"
        ///   Field:       "field:{pkg}:{typeFullName}::{fieldName}"
        ///   Literal:     "literal:{pkg}:{value[..100]}"
        ///   ArrayElement:"array:{containingMethodNodeId}:IL_{offset:X4}"
        ///   CallSite:    "callsite:{callerMethodNodeId}:IL_{offset:X4}"
        /// </summary>
        public int Id { get; set; }
        public DataFlowNodeType Type { get; set; }

        public string Name { get; set; } = string.Empty;
        public string TypeFullName { get; set; } = string.Empty;
        public int PackageId { get; set; }
        public string AssemblyName { get; set; } = string.Empty;

        // Id of the method containing methodFullName
        public int ContainingMethodNodeId { get; set; }

        // -- Only CallSite --
        public uint? InstructionOffset { get; set; }
    }
}