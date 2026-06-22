using dnlib.DotNet;
using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Infrastructure.Repositories.DataFlow.Analysis.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.Ensures.Interface;
using NuReaper.Infrastructure.Repositories.DataFlow.Processes.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.Registries;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Analysis
{
    internal sealed class AnalyzeMethod : IAnalyzeMethod
    {
        private readonly IProcessInstruction _processInstruction;
        private readonly IEnsuring _ensuring;

        public AnalyzeMethod(IProcessInstruction processInstruction, IEnsuring ensuring)
        {
            _processInstruction = processInstruction;
            _ensuring           = ensuring;
        }

        public void Execute(
            MethodDef method,
            TypeDef type,
            string packageKey,
            int packageId,
            ModuleDef module,
            MultiPackageResolver resolver,
            NodeRegistry registry,
            List<DataFlowEdge> edges)
        {
            // ── Węzeł metody ──────────────────────────────────────────────────────
            int callerNodeId = _ensuring.EnsureMethodNode(registry, method, packageId, module);

            // ── Pre-pass: locals → int[] ──────────────────────────────────────────
            int localCount = method.Body.Variables.Count;
            var localNodeIds = new int[localCount];
            for (int i = 0; i < localCount; i++)
                localNodeIds[i] = _ensuring.EnsureVariableNode(
                    registry, callerNodeId, type.FullName, packageId,
                    isArg: false, index: i);

            // ── Pre-pass: params → int[] ──────────────────────────────────────────
            int realParamCount = method.MethodSig?.Params.Count ?? 0;
            var paramNodeIds = new int[realParamCount]; // 0 = brak (sentinel)
            foreach (var param in method.Parameters)
            {
                int si = param.MethodSigIndex; // -1 = this → pomijamy
                if (si >= 0 && si < realParamCount)
                    paramNodeIds[si] = _ensuring.EnsureVariableNode(
                        registry, callerNodeId, type.FullName, packageId,
                        isArg: true, index: si);
            }

            // ── Forward pass ──────────────────────────────────────────────────────
            var evalStack = new Stack<StackSlot>(16);

            foreach (var instr in method.Body.Instructions)
            {
                _processInstruction.Execute(
                    instr, method, callerNodeId, packageId,
                    localNodeIds, paramNodeIds, resolver, registry, edges, evalStack);
            }
        }
    }
}