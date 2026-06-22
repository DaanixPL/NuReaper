using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.DataFlow.Analysis.InstructionAnalysis.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.Ensures.Interface;
using NuReaper.Infrastructure.Repositories.DataFlow.Handlers.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.Helpers;
using NuReaper.Infrastructure.Repositories.DataFlow.Helpers.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.Processes.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.Registries;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Processes
{
    internal sealed class ProcessInstruction : IProcessInstruction
    {
        private readonly IOpcodeHelpers _opcodeHelpers;
        private readonly IEnsuring _ensuring;
        private readonly IHandleCall _handleCall;

        public ProcessInstruction(IOpcodeHelpers opcodeHelpers, IEnsuring ensuring, IHandleCall handleCall)
        {
            _opcodeHelpers = opcodeHelpers;
            _ensuring      = ensuring;
            _handleCall    = handleCall;
        }

        public void Execute(
            Instruction instr,
            MethodDef method,
            int callerNodeId,
            int packageId,
            int[] localNodeIds,
            int[] paramNodeIds,
            MultiPackageResolver resolver,
            NodeRegistry registry,
            List<DataFlowEdge> edges,
            Stack<StackSlot> evalStack)
        {
            var op = instr.OpCode.Code;

            // ── ldloc_* / ldloc.s / ldloc ────────────────────────────────────────
            if (_opcodeHelpers.TryGetLocalIndex(instr, out int localIdx))
            {
                int nodeId = (localIdx >= 0 && localIdx < localNodeIds.Length)
                    ? localNodeIds[localIdx] : 0;
                evalStack.Push(new StackSlot { VarNodeId = nodeId });
                return;
            }

            // ── ldarg_* / ldarg.s / ldarg ─────────────────────────────────────────
            if (_opcodeHelpers.TryGetArgSigIndex(instr, method, out int sigIdx))
            {
                int nodeId = (sigIdx >= 0 && sigIdx < paramNodeIds.Length)
                    ? paramNodeIds[sigIdx] : 0;
                evalStack.Push(new StackSlot { VarNodeId = nodeId });
                return;
            }

            // ── stloc_* / stloc.s / stloc ────────────────────────────────────────
            if (_opcodeHelpers.TryGetStoreLocalIndex(instr, out int storeIdx))
            {
                var slot = evalStack.TryPop(out var s) ? s : StackSlot.Unknown;

                if (storeIdx >= 0 && storeIdx < localNodeIds.Length)
                {
                    int targetVarId = localNodeIds[storeIdx];

                    if (slot.CallResultOfId != 0)
                        edges.Add(EdgeFactory.Edge(slot.CallResultOfId, targetVarId, DataFlowEdgeType.Returns));
                    else if (slot.FieldNodeId != 0)
                        edges.Add(EdgeFactory.Edge(slot.FieldNodeId, targetVarId, DataFlowEdgeType.ReadsField));
                    else if (slot.VarNodeId != 0 && slot.VarNodeId != targetVarId)
                        edges.Add(EdgeFactory.Edge(slot.VarNodeId, targetVarId, DataFlowEdgeType.Assigns));
                    else if (slot.LiteralNodeId != 0)
                        edges.Add(EdgeFactory.Edge(slot.LiteralNodeId, targetVarId, DataFlowEdgeType.Assigns));
                    else if (slot.ArrayNodeId != 0)
                        edges.Add(EdgeFactory.Edge(slot.ArrayNodeId, targetVarId, DataFlowEdgeType.Assigns));
                }
                return;
            }

            // ── ldfld / ldsfld ────────────────────────────────────────────────────
            if (op == Code.Ldfld || op == Code.Ldsfld)
            {
                if (op == Code.Ldfld) evalStack.TryPop(out _);

                if (instr.Operand is IField fieldRef)
                    evalStack.Push(new StackSlot
                    {
                        FieldNodeId = _ensuring.EnsureFieldNode(registry, fieldRef, packageId, resolver)
                    });
                else
                    evalStack.Push(StackSlot.Unknown);
                return;
            }

            // ── stfld / stsfld ────────────────────────────────────────────────────
            if (op == Code.Stfld || op == Code.Stsfld)
            {
                var valueSlot = evalStack.TryPop(out var vs) ? vs : StackSlot.Unknown;
                if (op == Code.Stfld) evalStack.TryPop(out _);

                if (instr.Operand is IField fieldRef && valueSlot.AnyNodeId != 0)
                {
                    int fieldNodeId = _ensuring.EnsureFieldNode(registry, fieldRef, packageId, resolver);
                    edges.Add(EdgeFactory.Edge(valueSlot.AnyNodeId, fieldNodeId, DataFlowEdgeType.WritesField));
                }
                return;
            }

            // ── ldstr ─────────────────────────────────────────────────────────────
            if (op == Code.Ldstr && instr.Operand is string strVal)
            {
                evalStack.Push(new StackSlot
                {
                    LiteralNodeId = _ensuring.EnsureLiteralNode(registry, strVal, packageId)
                });
                return;
            }

            // ── newarr ────────────────────────────────────────────────────────────
            if (op == Code.Newarr)
            {
                evalStack.TryPop(out _); // rozmiar tablicy
                var callerNode = registry.GetById(callerNodeId);
                evalStack.Push(new StackSlot
                {
                    ArrayNodeId = _ensuring.EnsureArrayNode(
                        registry, instr,
                        callerNodeId, callerNode.TypeFullName, packageId)
                });
                return;
            }

            // ── stelem ────────────────────────────────────────────────────────────
            if (Checks.IsStelem(op))
            {
                var valueSlot = evalStack.TryPop(out var vsl) ? vsl : StackSlot.Unknown;
                evalStack.TryPop(out _); // indeks tablicy

                if (evalStack.TryPeek(out var arrSlot)
                    && arrSlot.ArrayNodeId != 0
                    && valueSlot.AnyNodeId != 0)
                {
                    edges.Add(EdgeFactory.Edge(valueSlot.AnyNodeId, arrSlot.ArrayNodeId, DataFlowEdgeType.Contains));
                }
                return;
            }

            // ── ldelem ────────────────────────────────────────────────────────────
            if (Checks.IsLdelem(op))
            {
                evalStack.TryPop(out _); // indeks
                var arrSlot = evalStack.TryPop(out var a) ? a : StackSlot.Unknown;
                evalStack.Push(new StackSlot { ArrayNodeId = arrSlot.ArrayNodeId });
                return;
            }

            // ── call / callvirt / newobj ──────────────────────────────────────────
            if (op == Code.Call || op == Code.Callvirt || op == Code.Newobj)
            {
                if (instr.Operand is IMethod calledMethod)
                    _handleCall.Execute(
                        instr, calledMethod, callerNodeId, packageId,
                        resolver, registry, edges, evalStack,
                        isNewobj: op == Code.Newobj);
                return;
            }

            // ── dup ───────────────────────────────────────────────────────────────
            if (op == Code.Dup)
            {
                if (evalStack.TryPeek(out var top)) evalStack.Push(top);
                return;
            }

            // ── pop ───────────────────────────────────────────────────────────────
            if (op == Code.Pop)
            {
                evalStack.TryPop(out _);
                return;
            }

            // ── ret ───────────────────────────────────────────────────────────────
            if (op == Code.Ret)
            {
                evalStack.Clear();
                return;
            }

            ApplyGenericStackEffect.Execute(op, evalStack);
        }
    }
}