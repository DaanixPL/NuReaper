using NuReaper.Domain.Enums;

namespace NuReaper.Domain.Entities.DataFlow
{
    public class DataFlowNode
    {
        /// <summary>
        /// Unique identifier for the node
        /// Format:
        ///   Method:      "method:{pkg}:{methodFullName}"
        ///   Variable:    "var:{methodFullName}:{pkg}:{local_N|arg_N}"
        ///   Field:       "field:{pkg}:{typeFullName}::{fieldName}"
        ///   Literal:     "literal:{pkg}:{value[..100]}"
        ///   ArrayElement:"array:{methodFullName}:{pkg}:IL_{offset:X4}"
        ///   CallSite:    "callsite:{callerMethodFullName}:{pkg}:IL_{offset:X4}"
        /// </summary>
        public string Id { get; set; } = string.Empty;

        public DataFlowNodeType Type { get; set; }

        public string Name { get; set; } = string.Empty;
        public string TypeFullName { get; set; } = string.Empty;
        public string PackageName { get; set; } = string.Empty;
        public string AssemblyName { get; set; } = string.Empty;

        // Id of the method containing methodFullName
        public string? ContainingMethodNodeId { get; set; }

        // -- Only CallSite --
        public uint? InstructionOffset { get; set; }
        public string? TargetMethodNodeId { get; set; }
    }
}