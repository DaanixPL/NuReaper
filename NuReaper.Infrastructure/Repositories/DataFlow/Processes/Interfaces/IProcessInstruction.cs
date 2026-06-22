using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Infrastructure.Repositories.DataFlow.Registries;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Processes.Interfaces
{
    internal interface IProcessInstruction
    {
        void Execute(
            Instruction instr,
            MethodDef method,
            int callerNodeId,
            int packageId,
            int[] localNodeIds,
            int[] paramNodeIds,
            MultiPackageResolver resolver,
            NodeRegistry registry,
            List<DataFlowEdge> edges,
            Stack<StackSlot> evalStack);
    }
}