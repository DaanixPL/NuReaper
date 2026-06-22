using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Infrastructure.Repositories.DataFlow.Registries;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Handlers.Interfaces
{
    internal interface IHandleCall
    {
        internal void Execute(
            Instruction instr,
            IMethod calledMethod,
            int callerNodeId,
            int callerPackageId,
            MultiPackageResolver resolver,
            NodeRegistry nodeRegistry,
            List<DataFlowEdge> edges,
            Stack<StackSlot> evalStack,
            bool isNewobj);
    }
}
