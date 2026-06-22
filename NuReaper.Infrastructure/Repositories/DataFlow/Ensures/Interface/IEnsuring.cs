using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Infrastructure.Repositories.DataFlow.Registries;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Ensures.Interface
{
    public interface IEnsuring
    {
        public int EnsureFieldNode(
            NodeRegistry nodeRegistry,
            IField fieldRef,
            int callerPackageId,
            MultiPackageResolver resolver);
        public int EnsureLiteralNode(
            NodeRegistry nodeRegistry,
            string value,
            int packageId);
        public int EnsureArrayNode(
            NodeRegistry nodeRegistry,
            Instruction instr,
            int containingMethodNodeId,
            string typeFullName,
            int packageId);
        public int EnsureCallSiteNode(
            NodeRegistry nodeRegistry,
            Instruction instr,
            DataFlowNode callerNode,
            DataFlowNode calleeNode,
            int packageId);
        public int EnsureMethodNodeFromRef(
            NodeRegistry nodeRegistry,
            IMethod calledMethod,
            int packageId);
        public int EnsureMethodNode(
            NodeRegistry nodeRegistry,
            MethodDef method,
            int packageId,
            ModuleDef module);
        public int EnsureVariableNode(
            NodeRegistry nodeRegistry,
            int containingMethodNodeId,
            string typeFullName,
            int packageId,
            bool isArg,
            int index);
    }
}
