using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.DataFlow.Ensures.Interface;
using NuReaper.Infrastructure.Repositories.DataFlow.Helpers;
using NuReaper.Infrastructure.Repositories.DataFlow.Registries;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Ensures
{
    public class Ensuring : IEnsuring
    {
        public int EnsureArrayNode(NodeRegistry nodeRegistry, Instruction instr, int containingMethodNodeId, string typeFullName, int packageId)
        {
            string key = $"array:{containingMethodNodeId}:IL_{instr.Offset:X4}";

            return nodeRegistry.GetOrCreate(key, id => new DataFlowNode
            {
                Id                     = id,
                Type                   = DataFlowNodeType.ArrayElement,
                Name                   = $"array@IL_{instr.Offset:X4}",
                TypeFullName           = typeFullName,
                PackageId            = packageId,
                ContainingMethodNodeId = containingMethodNodeId,
                InstructionOffset      = instr.Offset
            });
        }

        public int EnsureCallSiteNode(NodeRegistry nodeRegistry, Instruction instr, DataFlowNode callerNode, DataFlowNode calleeNode, int packageId)
        {
            string key = $"callsite:{callerNode.Id}:IL_{instr.Offset:X4}";

            return nodeRegistry.GetOrCreate(key, id => new DataFlowNode
            {
                Id                     = id,
                Type                   = DataFlowNodeType.CallSite,
                Name                   = $"{callerNode.Name} → {calleeNode.Name} @ IL_{instr.Offset:X4}",
                TypeFullName           = calleeNode.TypeFullName,
                PackageId            = packageId,
                ContainingMethodNodeId = callerNode.Id,
                InstructionOffset      = instr.Offset,
            });
        }

        public int EnsureFieldNode(NodeRegistry nodeRegistry, IField fieldRef, int callerPackageId, MultiPackageResolver resolver)
        {
            var asmName = fieldRef.DeclaringType?.DefinitionAssembly?.Name.String;
            var pkgId = asmName != null ? resolver.ResolvePackageId(asmName) : callerPackageId;
            var typeName = fieldRef.FullName ?? "<unknown>";
            string key = $"field:{pkgId}:{typeName}::{fieldRef.Name}";
            return nodeRegistry.GetOrCreate(key, id => new DataFlowNode
            {
                Id           = id,
                Type         = DataFlowNodeType.Field,
                Name         = fieldRef.Name,
                TypeFullName = typeName,
                PackageId    = pkgId,
                AssemblyName = asmName ?? string.Empty
            });
        }

        public int EnsureLiteralNode(NodeRegistry nodeRegistry, string value, int packageId)
        {
            string keyValue = value.Length > 120 ? value[..120] : value;
            string key       = $"literal:{packageId}:{keyValue}";

            return nodeRegistry.GetOrCreate(key, id => new DataFlowNode
            {
                Id          = id,
                Type        = DataFlowNodeType.Literal,
                Name        = value,        // pełna wartość
                PackageId   = packageId
            });
        }

        public int EnsureMethodNode(NodeRegistry nodeRegistry, MethodDef method, int packageId, ModuleDef module)
        {
            string key = MethodNodeId.Execute(packageId, method.FullName);

            return nodeRegistry.GetOrCreate(key, id => new DataFlowNode
            {
                Id           = id,
                Type         = DataFlowNodeType.Method,
                Name         = method.Name,
                TypeFullName = method.FullName ?? string.Empty,
                PackageId    = packageId,
                AssemblyName = module.Assembly?.Name.String ?? string.Empty
            });
        }

        public int EnsureMethodNodeFromRef(NodeRegistry nodeRegistry, IMethod calledMethod, int packageId)
        {
            string key = MethodNodeId.Execute(packageId, calledMethod.FullName);
            return nodeRegistry.GetOrCreate(key, id => new DataFlowNode
            {
                Id           = id,
                Type         = DataFlowNodeType.Method,
                Name         = calledMethod.Name,
                TypeFullName = calledMethod.FullName ?? string.Empty,
                PackageId    = packageId,
                AssemblyName = calledMethod.DeclaringType?.DefinitionAssembly?.Name.String ?? string.Empty
            });
        }

        public int EnsureVariableNode(NodeRegistry nodeRegistry, int containingMethodNodeId, string typeFullName, int packageId, bool isArg, int index)
        {
            string varName = isArg ? $"arg_{index}" : $"local_{index}";
            string key      = $"var:{containingMethodNodeId}:{varName}";

            return nodeRegistry.GetOrCreate(key, id => new DataFlowNode
            {
                Id                     = id,
                Type                   = DataFlowNodeType.Variable,
                Name                   = varName,
                TypeFullName           = typeFullName,
                PackageId              = packageId,
                ContainingMethodNodeId = containingMethodNodeId
            });
        }
    }
}
