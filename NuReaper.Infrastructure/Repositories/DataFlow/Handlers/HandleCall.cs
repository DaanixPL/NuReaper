using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.DataFlow.Ensures.Interface;
using NuReaper.Infrastructure.Repositories.DataFlow.Handlers.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.Helpers;
using NuReaper.Infrastructure.Repositories.DataFlow.Registries;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Handlers
{
    internal class HandleCall : IHandleCall
    {
        private readonly IEnsuring _ensuring; 

        public HandleCall(IEnsuring ensuring)
        {
            _ensuring = ensuring;
        }
        public void Execute(Instruction instr, IMethod calledMethod, int callerNodeId, int callerPackageId, MultiPackageResolver resolver, NodeRegistry nodes, List<DataFlowEdge> edges, Stack<StackSlot> evalStack, bool isNewobj)
        {
            var sig = calledMethod.MethodSig;

            // newobj: nie ma implicit this na stosie (tworzy nowy obiekt)
            // call/callvirt na instance method: this jest na stosie → dodatkowy pop
            bool hasImplicitThis = !isNewobj && (sig?.HasThis ?? false);
            int  paramCount      = sig?.Params.Count ?? 0;
            int  totalPop        = paramCount + (hasImplicitThis ? 1 : 0);

            // Zdejmij argumenty ze stosu (ostatni push = ostatni arg = poppedSlots[totalPop-1])
            var poppedSlots = new StackSlot[totalPop];
            for (int i = totalPop - 1; i >= 0; i--)
                poppedSlots[i] = evalStack.TryPop(out var s) ? s : StackSlot.Unknown;

            // Węzeł wywoływanej metody
            var targetPkgId = ResolvePackage.Execute(calledMethod, resolver, callerPackageId);
            int calleeNodeId = _ensuring.EnsureMethodNodeFromRef(nodes, calledMethod, targetPkgId);

            var callerNode = nodes.GetById(callerNodeId);
            var calleeNode = nodes.GetById(calleeNodeId);
            // Węzeł CallSite — unikalne: caller + offset IL
            var callSiteNodeId = _ensuring.EnsureCallSiteNode(nodes, instr, callerNode, calleeNode, callerPackageId);

            // ── Krawędź: Caller ──Calls──► CallSite ──────────────────────────────
            edges.Add(EdgeFactory.Edge(callerNodeId, callSiteNodeId, DataFlowEdgeType.Calls));

            // ── Krawędź: CallSite ──Targets──► Callee ────────────────────────────
            edges.Add(EdgeFactory.Edge(callSiteNodeId, calleeNodeId, DataFlowEdgeType.Targets));

            // ── Krawędź: this/instance ──FlowsInto──► CallSite ───────────────────
            // Dla fluent chains typu GetByteArrayAsync(...).Result pozwala to
            // zachować przepływ danych przez implicit this.
            if (hasImplicitThis && poppedSlots.Length > 0)
            {
                var instanceSourceNodeId = poppedSlots[0].AnyNodeId;
                if (instanceSourceNodeId != 0)
                {
                    edges.Add(new DataFlowEdge
                    {
                        FromId        = instanceSourceNodeId,
                        ToId          = callSiteNodeId,
                        EdgeType      = DataFlowEdgeType.FlowInto,
                        ArgumentIndex = -1
                    });
                }
            }

            // ── Krawędź: Argument ──FlowsInto──► CallSite ─────────────────────��──
            // Slot 0 = this (jeśli hasImplicitThis) → pomijamy (argStart=1)
            // Slot 1+ = rzeczywiste argumenty, ArgumentIndex = 0-based bez this
            int argStart = hasImplicitThis ? 1 : 0;
            for (int i = argStart; i < poppedSlots.Length; i++)
            {
                var sourceNodeId = poppedSlots[i].AnyNodeId;
                if (sourceNodeId == 0) continue;

                edges.Add(new DataFlowEdge
                {
                    FromId        = sourceNodeId,
                    ToId          = callSiteNodeId,
                    EdgeType      = DataFlowEdgeType.FlowInto,
                    ArgumentIndex = i - argStart   // 0-based, bez this
                });
            }

            // ── Wynik na stos (jeśli metoda coś zwraca) ──────────────────────────
            bool returnsVoid = sig?.RetType?.ElementType == ElementType.Void;
            if (!returnsVoid || isNewobj)
                evalStack.Push(new StackSlot { CallResultOfId = callSiteNodeId });
        }
    }
}
