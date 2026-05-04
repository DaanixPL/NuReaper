using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Microsoft.Extensions.Logging;
using NuReaper.Application.DTOs.DataFlow;
using NuReaper.Application.Interfaces.DataFlow;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.DataFlow;

namespace NuReaper.Infrastructure.Repositories.DataFlowAnalysis
{
    /// <summary>
    /// Buduje DataFlowGraph analizując IL z podanych .dll.
    ///
    /// Algorytm per-metoda:
    ///   1. Pre-pass:    utwórz węzły dla wszystkich locals i params (raz, O(n))
    ///   2. Forward pass: symuluj evaluation stack, buduj węzły/krawędzie w locie (O(n))
    ///   3. Post-pass:   BindParameters — połącz argumenty call-site z param arg_N target metody
    ///
    /// Cross-package: MultiPackageResolver sprawia, że MemberRef → MethodDef działa między DLL.
    /// </summary>
    public sealed class DataFlowGraphBuilder : IDataFlowGraphBuilder
    {
        private readonly ILogger<DataFlowGraphBuilder> _logger;

        public DataFlowGraphBuilder(ILogger<DataFlowGraphBuilder> logger)
            => _logger = logger;

        // ════════════════════════════════════════════════════════════════════════
        // Public API
        // ════════════════════════════════════════════════════════════════════════

        public DataFlowGraphDto Build(
            IReadOnlyList<(string packageName, string dllPath)> inputs,
            CancellationToken cancellationToken = default)
        {
            // ── 1. Wspólny resolver + ładowanie modułów ───────────────────────────
            // WAŻNE: najpierw ładujemy WSZYSTKIE moduły, potem analizujemy.
            // Gdybyśmy analizowali podczas ładowania, resolver nie znałby jeszcze
            // wszystkich assembly i cross-package Resolve() by failowało.
            var resolver  = new MultiPackageResolver();
            var moduleCtx = new ModuleContext(resolver);
            var loaded    = new List<(string pkg, ModuleDefMD module)>(inputs.Count);
            var deduplicatedInputs = inputs
                .GroupBy(x => x.dllPath, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            foreach (var (pkg, path) in deduplicatedInputs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var module = ModuleDefMD.Load(path, moduleCtx);
                    resolver.Register(module, pkg);
                    loaded.Add((pkg, module));
                    _logger.LogDebug("Loaded '{Asm}' → '{Pkg}'", module.Assembly?.Name.String, pkg);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cannot load {Path} (package: {Pkg})", path, pkg);
                }
            }

            // ── 2. Analiza IL ─────────────────────────────────────────────────────
            var nodes = new Dictionary<string, DataFlowNodeDto>();
            var edges = new List<DataFlowEdgeDto>();

            foreach (var (pkg, module) in loaded)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var type in module.GetTypes())
                {
                    foreach (var method in type.Methods)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!method.HasBody || !method.Body.HasInstructions) continue;

                        AnalyzeMethod(method, type, pkg, module, resolver, nodes, edges);
                    }
                }
            }

            // ── 3. Post-processing: połącz argumenty z parametrami ────────────────
            BindParameters(nodes, edges);

            // ── 4. Cleanup ────────────────────────────────────────────────────────
            foreach (var (_, module) in loaded)
                module.Dispose();

            return new DataFlowGraphDto
            {
                AnalyzedPackages = deduplicatedInputs.Select(x => x.packageName).Distinct().ToList(),
                Nodes            = nodes.Values.ToList(),
                Edges            = edges,
                GeneratedAt      = DateTime.UtcNow
            };
        }

        // ════════════════════════════════════════════════════════════════════════
        // Analiza metody
        // ════════════════════════════════════════════════════════════════════════

        private void AnalyzeMethod(
            MethodDef method,
            TypeDef type,
            string packageName,
            ModuleDef module,
            MultiPackageResolver resolver,
            Dictionary<string, DataFlowNodeDto> nodes,
            List<DataFlowEdgeDto> edges)
        {
            var callerNode = EnsureMethodNode(nodes, method, packageName, module);

            // ── Pre-pass: utwórz węzły dla locals ────────────────────────────────
            // localNodes[i] = węzeł dla local variable o indeksie i
            var localNodes = new DataFlowNodeDto[method.Body.Variables.Count];
            for (int i = 0; i < method.Body.Variables.Count; i++)
                localNodes[i] = EnsureVariableNode(nodes, callerNode.Id, type.FullName, packageName,
                                                   isArg: false, index: i);

            // ── Pre-pass: utwórz węzły dla parametrów ────────────────────────────
            // paramNodes[sigIdx] = węzeł dla parametru o MethodSigIndex = sigIdx (0-based, bez this)
            // MethodSigIndex = -1 oznacza 'this' → pomijamy
            int realParamCount = method.MethodSig?.Params.Count ?? 0;
            var paramNodes = new DataFlowNodeDto?[realParamCount];
            foreach (var param in method.Parameters)
            {
                int si = param.MethodSigIndex; // -1 dla this
                if (si >= 0 && si < realParamCount)
                    paramNodes[si] = EnsureVariableNode(nodes, callerNode.Id, type.FullName, packageName,
                                                        isArg: true, index: si);
            }

            // ── Forward pass z symulacją evaluation stack ─────────────────────────
            var evalStack = new Stack<StackSlot>(16);

            foreach (var instr in method.Body.Instructions)
            {
                ProcessInstruction(
                    instr, method, callerNode, packageName,
                    localNodes, paramNodes, resolver, nodes, edges, evalStack);
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // Przetwarzanie instrukcji
        // ════════════════════════════════════════════════════════════════════════

        private void ProcessInstruction(
            Instruction instr,
            MethodDef method,
            DataFlowNodeDto callerNode,
            string packageName,
            DataFlowNodeDto[] localNodes,
            DataFlowNodeDto?[] paramNodes,
            MultiPackageResolver resolver,
            Dictionary<string, DataFlowNodeDto> nodes,
            List<DataFlowEdgeDto> edges,
            Stack<StackSlot> evalStack)
        {
            var op = instr.OpCode.Code;

            // ── ldloc_* / ldloc.s / ldloc ────────────────────────────────────────
            if (TryGetLocalIndex(instr, out int localIdx))
            {
                var node = (localIdx >= 0 && localIdx < localNodes.Length)
                    ? localNodes[localIdx] : null;
                evalStack.Push(new StackSlot { VarNode = node });
                return;
            }

            // ── ldarg_* / ldarg.s / ldarg ─────────────────────────────────────────
            // TryGetArgSigIndex zwraca MethodSigIndex (-1 = this → skip)
            if (TryGetArgSigIndex(instr, method, out int sigIdx))
            {
                var node = (sigIdx >= 0 && sigIdx < paramNodes.Length)
                    ? paramNodes[sigIdx] : null;
                evalStack.Push(new StackSlot { VarNode = node });
                return;
            }

            // ── stloc_* / stloc.s / stloc ────────────────────────────────────────
            if (TryGetStoreLocalIndex(instr, out int storeIdx))
            {
                var slot = evalStack.TryPop(out var s) ? s : StackSlot.Unknown;

                if (storeIdx >= 0 && storeIdx < localNodes.Length)
                {
                    var targetVar = localNodes[storeIdx];

                    if (slot.CallResultOf != null)
                    {
                        // CallSite ──Returns──► Variable
                        edges.Add(Edge(slot.CallResultOf.Id, targetVar.Id, DataFlowEdgeType.Returns));
                    }
                    else if (slot.FieldNode != null)
                    {
                        // Field ──ReadsField──► Variable (ldfld był wcześniej na stosie)
                        edges.Add(Edge(slot.FieldNode.Id, targetVar.Id, DataFlowEdgeType.ReadsField));
                    }
                    else if (slot.VarNode != null && slot.VarNode.Id != targetVar.Id)
                    {
                        // Variable ──Assigns──► Variable (kopia)
                        edges.Add(Edge(slot.VarNode.Id, targetVar.Id, DataFlowEdgeType.Assigns));
                    }
                    else if (slot.LiteralNode != null)
                    {
                        // Literal ──Assigns──► Variable
                        edges.Add(Edge(slot.LiteralNode.Id, targetVar.Id, DataFlowEdgeType.Assigns));
                    }
                    else if (slot.ArrayNode != null)
                    {
                        // ArrayElement ──Assigns──► Variable
                        edges.Add(Edge(slot.ArrayNode.Id, targetVar.Id, DataFlowEdgeType.Assigns));
                    }
                }
                return;
            }

            // ── ldfld / ldsfld ────────────────────────────────────────────────────
            if (op == Code.Ldfld || op == Code.Ldsfld)
            {
                if (op == Code.Ldfld) evalStack.TryPop(out _); // pop object (this)

                if (instr.Operand is IField fieldRef)
                    evalStack.Push(new StackSlot
                    {
                        FieldNode = EnsureFieldNode(nodes, fieldRef, packageName, resolver)
                    });
                else
                    evalStack.Push(StackSlot.Unknown);
                return;
            }

            // ── stfld / stsfld ────────────────────────────────────────────────────
            if (op == Code.Stfld || op == Code.Stsfld)
            {
                var valueSlot = evalStack.TryPop(out var vs) ? vs : StackSlot.Unknown;
                if (op == Code.Stfld) evalStack.TryPop(out _); // pop object (this)

                if (instr.Operand is IField fieldRef && valueSlot.AnyNode != null)
                {
                    var fieldNode = EnsureFieldNode(nodes, fieldRef, packageName, resolver);
                    edges.Add(Edge(valueSlot.AnyNode.Id, fieldNode.Id, DataFlowEdgeType.WritesField));
                }
                return;
            }

            // ── ldstr ─────────────────────────────────────────────────────────────
            if (op == Code.Ldstr && instr.Operand is string strVal)
            {
                evalStack.Push(new StackSlot
                {
                    LiteralNode = EnsureLiteralNode(nodes, strVal, packageName)
                });
                return;
            }

            // ── newarr ─────────────────────���──────────────────────────────────────
            if (op == Code.Newarr)
            {
                evalStack.TryPop(out _); // rozmiar tablicy
                evalStack.Push(new StackSlot
                {
                    ArrayNode = EnsureArrayNode(nodes, instr, callerNode.Id,
                                               callerNode.TypeFullName, packageName)
                });
                return;
            }

            // ── stelem (wpisywanie wartości do tablicy) ───────────────────────────
            if (IsStelem(op))
            {
                var valueSlot = evalStack.TryPop(out var vsl) ? vsl : StackSlot.Unknown;
                evalStack.TryPop(out _); // indeks tablicy
                // Tablica ZOSTAJE na stosie (peek) — stelem nie zdejmuje arrref
                if (evalStack.TryPeek(out var arrSlot)
                    && arrSlot.ArrayNode != null
                    && valueSlot.AnyNode != null)
                {
                    edges.Add(Edge(valueSlot.AnyNode.Id, arrSlot.ArrayNode.Id, DataFlowEdgeType.Contains));
                }
                return;
            }

            // ── ldelem (czytanie z tablicy) ───────────────────────────────────────
            if (IsLdelem(op))
            {
                evalStack.TryPop(out _); // indeks
                var arrSlot = evalStack.TryPop(out var a) ? a : StackSlot.Unknown;
                // Propagujemy węzeł tablicy — jeśli trafi do call, FlowsInto go złapie
                evalStack.Push(new StackSlot { ArrayNode = arrSlot.ArrayNode });
                return;
            }

            // ── call / callvirt / newobj ──────────────────────────────────────────
            if (op == Code.Call || op == Code.Callvirt || op == Code.Newobj)
            {
                if (instr.Operand is IMethod calledMethod)
                    HandleCall(instr, calledMethod, callerNode, packageName,
                               resolver, nodes, edges, evalStack, isNewobj: op == Code.Newobj);
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

            // ── Pozostałe (arytmetyka, konwersje, branching...) ───────────────────
            // Dla instrukcji których nie śledzimy szczegółowo: symulujemy efekt stosu
            // żeby stos się nie "rozjechał" i dalsze instrukcje miały poprawne sloty.
            ApplyGenericStackEffect(op, evalStack);
        }

        // ════════════════════════════════════════════════════════════════════════
        // Obsługa call
        // ══════════��═════════════════════════════════════════════════════════════

        private void HandleCall(
            Instruction instr,
            IMethod calledMethod,
            DataFlowNodeDto callerNode,
            string callerPackage,
            MultiPackageResolver resolver,
            Dictionary<string, DataFlowNodeDto> nodes,
            List<DataFlowEdgeDto> edges,
            Stack<StackSlot> evalStack,
            bool isNewobj)
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
            var targetPkg  = ResolvePackage(calledMethod, resolver);
            var calleeNode = EnsureMethodNodeFromRef(nodes, calledMethod, targetPkg);

            // Węzeł CallSite — unikalne: caller + offset IL
            var callSiteNode = EnsureCallSiteNode(nodes, instr, callerNode, calleeNode, callerPackage);

            // ── Krawędź: Caller ──Calls──► CallSite ──────────────────────────────
            edges.Add(Edge(callerNode.Id, callSiteNode.Id, DataFlowEdgeType.Calls));

            // ── Krawędź: CallSite ──Targets──► Callee ────────────────────────────
            edges.Add(Edge(callSiteNode.Id, calleeNode.Id, DataFlowEdgeType.Targets));

            // ── Krawędź: Argument ──FlowsInto──► CallSite ─────────────────────��──
            // Slot 0 = this (jeśli hasImplicitThis) → pomijamy (argStart=1)
            // Slot 1+ = rzeczywiste argumenty, ArgumentIndex = 0-based bez this
            int argStart = hasImplicitThis ? 1 : 0;
            for (int i = argStart; i < poppedSlots.Length; i++)
            {
                var sourceNode = poppedSlots[i].AnyNode;
                if (sourceNode == null) continue;

                edges.Add(new DataFlowEdgeDto
                {
                    FromId        = sourceNode.Id,
                    ToId          = callSiteNode.Id,
                    EdgeType      = DataFlowEdgeType.FlowInto,
                    ArgumentIndex = i - argStart   // 0-based, bez this
                });
            }

            // ── Wynik na stos (jeśli metoda coś zwraca) ──────────────────────────
            bool returnsVoid = sig?.RetType?.ElementType == ElementType.Void;
            if (!returnsVoid || isNewobj)
                evalStack.Push(new StackSlot { CallResultOf = callSiteNode });
        }

        // ════════════════════════════════════════════════════════════════════════
        // Post-processing: ParameterBinding
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Łączy argumenty call-site z parametrami wewnątrz wywoływanej metody.
        ///
        /// Dla każdej pary krawędzi:
        ///   X ──FlowsInto(argIdx=N)──► CallSite ──Targets──► MethodB
        ///
        /// Dodaje:
        ///   CallSite ──ParameterBinding(N)──► arg_N (wewnątrz MethodB)
        ///
        /// Dzięki temu wartość X jest "widoczna" wewnątrz MethodB jako arg_N,
        /// i dalszy przepływ (arg_N → inny call) jest śledzony bez przerwy.
        /// </summary>
        private static void BindParameters(
            Dictionary<string, DataFlowNodeDto> nodes,
            List<DataFlowEdgeDto> edges)
        {
            // callSiteId → targetMethodId
            var callSiteToTarget = edges
                .Where(e => e.EdgeType == DataFlowEdgeType.Targets)
                .GroupBy(e => e.FromId)
                .ToDictionary(g => g.Key, g => g.First().ToId);

            // methodId (ContainingMethodNodeId) → { sigIndex → węzeł arg_N }
            var methodToParams = nodes.Values
                .Where(n => n.Type  == DataFlowNodeType.Variable
                         && n.Name.StartsWith("arg_", StringComparison.Ordinal)
                         && n.ContainingMethodNodeId != null)
                .GroupBy(n => n.ContainingMethodNodeId!)
                .ToDictionary(
                    g => g.Key,
                    g => g.ToDictionary(n => ParseArgIndex(n.Name), n => n));

            // Snapshot — iterujemy po istniejących, dodajemy nowe
            var flowEdges = edges
                .Where(e => e.EdgeType == DataFlowEdgeType.FlowInto
                         && e.ArgumentIndex.HasValue)
                .ToList();

            foreach (var flow in flowEdges)
            {
                // flow.ToId = CallSite
                if (!callSiteToTarget.TryGetValue(flow.ToId, out var targetMethodId)) continue;
                if (!methodToParams.TryGetValue(targetMethodId, out var paramMap))    continue;
                if (!paramMap.TryGetValue(flow.ArgumentIndex!.Value, out var param))  continue;

                edges.Add(new DataFlowEdgeDto
                {
                    FromId        = flow.ToId,      // CallSite
                    ToId          = param.Id,        // arg_N w target metodzie
                    EdgeType      = DataFlowEdgeType.ParameterBinding,
                    ArgumentIndex = flow.ArgumentIndex
                });
            }
        }

        // ════════════════════════════════════════════════════════════════════════
        // EnsureXxx — węzły (idempotentne, deduplikowane przez nodes dict)
        // ════════════════════════════════════════════════════════════════════════

        private static DataFlowNodeDto EnsureMethodNode(
            Dictionary<string, DataFlowNodeDto> nodes,
            MethodDef method,
            string packageName,
            ModuleDef module)
        {
            // FullName = "System.Void Namespace.Type::Name(Param1,Param2)" — globalnie unikalny
            string id = MethodNodeId(packageName, method.FullName);
            if (nodes.TryGetValue(id, out var n)) return n;

            var node = new DataFlowNodeDto
            {
                Id           = id,
                Type         = DataFlowNodeType.Method,
                Name         = method.Name,
                TypeFullName = method.DeclaringType?.FullName ?? string.Empty,
                PackageName  = packageName,
                AssemblyName = module.Assembly?.Name.String ?? string.Empty
            };
            nodes[id] = node;
            return node;
        }

        private static DataFlowNodeDto EnsureMethodNodeFromRef(
            Dictionary<string, DataFlowNodeDto> nodes,
            IMethod calledMethod,
            string packageName)
        {
            // FullName jest takie samo dla MemberRef i MethodDef tego samego sygnatura
            string id = MethodNodeId(packageName, calledMethod.FullName);
            if (nodes.TryGetValue(id, out var n)) return n;

            var node = new DataFlowNodeDto
            {
                Id           = id,
                Type         = DataFlowNodeType.Method,
                Name         = calledMethod.Name,
                TypeFullName = calledMethod.DeclaringType?.FullName ?? string.Empty,
                PackageName  = packageName,
                AssemblyName = calledMethod.DeclaringType?.DefinitionAssembly?.Name.String ?? string.Empty
            };
            nodes[id] = node;
            return node;
        }

        private static DataFlowNodeDto EnsureVariableNode(
            Dictionary<string, DataFlowNodeDto> nodes,
            string containingMethodNodeId,
            string typeFullName,
            string packageName,
            bool isArg,
            int index)
        {
            // index: dla locals = indeks zmiennej, dla args = MethodSigIndex (0-based, bez this)
            string varName = isArg ? $"arg_{index}" : $"local_{index}";
            string id      = $"var:{containingMethodNodeId}:{varName}";

            if (nodes.TryGetValue(id, out var n)) return n;

            var node = new DataFlowNodeDto
            {
                Id                     = id,
                Type                   = DataFlowNodeType.Variable,
                Name                   = varName,
                TypeFullName           = typeFullName,
                PackageName            = packageName,
                ContainingMethodNodeId = containingMethodNodeId
            };
            nodes[id] = node;
            return node;
        }

        private static DataFlowNodeDto EnsureFieldNode(
            Dictionary<string, DataFlowNodeDto> nodes,
            IField fieldRef,
            string callerPackage,
            MultiPackageResolver resolver)
        {
            var asmName = fieldRef.DeclaringType?.DefinitionAssembly?.Name.String;
            var pkg     = asmName != null ? resolver.ResolvePackageName(asmName) : callerPackage;
            var typeName = fieldRef.DeclaringType?.FullName ?? "<unknown>";

            string id = $"field:{pkg}:{typeName}::{fieldRef.Name}";
            if (nodes.TryGetValue(id, out var n)) return n;

            var node = new DataFlowNodeDto
            {
                Id           = id,
                Type         = DataFlowNodeType.Field,
                Name         = fieldRef.Name,
                TypeFullName = typeName,
                PackageName  = pkg,
                AssemblyName = asmName ?? string.Empty
            };
            nodes[id] = node;
            return node;
        }

        private static DataFlowNodeDto EnsureLiteralNode(
            Dictionary<string, DataFlowNodeDto> nodes,
            string value,
            string packageName)
        {
            // Deduplikacja po wartości — ten sam string w różnych miejscach = jeden węzeł.
            // Dla długich stringów skracamy klucz (nie chcemy 10KB w kluczu słownika).
            string keyValue = value.Length > 120 ? value[..120] : value;
            string id       = $"literal:{packageName}:{keyValue}";

            if (nodes.TryGetValue(id, out var n)) return n;

            var node = new DataFlowNodeDto
            {
                Id          = id,
                Type        = DataFlowNodeType.Literal,
                Name        = value,        // pełna wartość
                PackageName = packageName
            };
            nodes[id] = node;
            return node;
        }

        private static DataFlowNodeDto EnsureArrayNode(
            Dictionary<string, DataFlowNodeDto> nodes,
            Instruction instr,
            string containingMethodNodeId,
            string typeFullName,
            string packageName)
        {
            // Każde newarr = osobna tablica. Unikalny per (metoda, offset IL).
            string id = $"array:{containingMethodNodeId}:IL_{instr.Offset:X4}";

            if (nodes.TryGetValue(id, out var n)) return n;

            var node = new DataFlowNodeDto
            {
                Id                     = id,
                Type                   = DataFlowNodeType.ArrayElement,
                Name                   = $"array@IL_{instr.Offset:X4}",
                TypeFullName           = typeFullName,
                PackageName            = packageName,
                ContainingMethodNodeId = containingMethodNodeId
            };
            nodes[id] = node;
            return node;
        }

        private static DataFlowNodeDto EnsureCallSiteNode(
            Dictionary<string, DataFlowNodeDto> nodes,
            Instruction instr,
            DataFlowNodeDto callerNode,
            DataFlowNodeDto calleeNode,
            string packageName)
        {
            // Unikalny per (caller metoda, offset IL). Ta sama metoda może wielokrotnie
            // wywoływać ten sam cel w pętli — każde wywołanie to osobny CallSite.
            string id = $"callsite:{callerNode.Id}:IL_{instr.Offset:X4}";

            if (nodes.TryGetValue(id, out var n)) return n;

            var node = new DataFlowNodeDto
            {
                Id                     = id,
                Type                   = DataFlowNodeType.CallSite,
                Name                   = $"{callerNode.Name} → {calleeNode.Name} @ IL_{instr.Offset:X4}",
                TypeFullName           = callerNode.TypeFullName,
                PackageName            = packageName,
                ContainingMethodNodeId = callerNode.Id,
                InstructionOffset      = instr.Offset,
                TargetMethodNodeId     = calleeNode.Id
            };
            nodes[id] = node;
            return node;
        }

        // ════════════════════════════════════════════════════════════════════════
        // IL opcode helpers
        // ════════════════════════════════════════════════════════════════════════

        private static bool TryGetLocalIndex(Instruction instr, out int index)
        {
            switch (instr.OpCode.Code)
            {
                case Code.Ldloc_0: index = 0; return true;
                case Code.Ldloc_1: index = 1; return true;
                case Code.Ldloc_2: index = 2; return true;
                case Code.Ldloc_3: index = 3; return true;
                case Code.Ldloc_S:
                case Code.Ldloc:
                    if (instr.Operand is Local loc) { index = loc.Index; return true; }
                    break;
            }
            index = -1;
            return false;
        }

        private static bool TryGetStoreLocalIndex(Instruction instr, out int index)
        {
            switch (instr.OpCode.Code)
            {
                case Code.Stloc_0: index = 0; return true;
                case Code.Stloc_1: index = 1; return true;
                case Code.Stloc_2: index = 2; return true;
                case Code.Stloc_3: index = 3; return true;
                case Code.Stloc_S:
                case Code.Stloc:
                    if (instr.Operand is Local loc) { index = loc.Index; return true; }
                    break;
            }
            index = -1;
            return false;
        }

        /// <summary>
        /// Zwraca MethodSigIndex parametru (-1 = this → caller powinien pominąć).
        /// MethodSigIndex: 0 = pierwszy rzeczywisty parametr (bez this), niezależnie od static/instance.
        /// </summary>
        private static bool TryGetArgSigIndex(Instruction instr, MethodDef method, out int sigIndex)
        {
            int rawParamIdx;
            switch (instr.OpCode.Code)
            {
                case Code.Ldarg_0: rawParamIdx = 0; break;
                case Code.Ldarg_1: rawParamIdx = 1; break;
                case Code.Ldarg_2: rawParamIdx = 2; break;
                case Code.Ldarg_3: rawParamIdx = 3; break;
                case Code.Ldarg_S:
                case Code.Ldarg:
                    // dnlib rozwiązuje operand do obiektu Parameter
                    if (instr.Operand is Parameter p)
                    {
                        sigIndex = p.MethodSigIndex; // -1 jeśli to 'this'
                        return sigIndex >= 0;
                    }
                    sigIndex = -1;
                    return false;
                default:
                    sigIndex = -1;
                    return false;
            }

            // Dla ldarg_0..3: pobierz Parameter z listy metody i sprawdź MethodSigIndex
            if (rawParamIdx < method.Parameters.Count)
            {
                sigIndex = method.Parameters[rawParamIdx].MethodSigIndex;
                return sigIndex >= 0; // -1 = this → false
            }

            sigIndex = -1;
            return false;
        }

        private static bool IsStelem(Code op) => op
            is Code.Stelem_Ref or Code.Stelem_I   or Code.Stelem_I1
            or Code.Stelem_I2  or Code.Stelem_I4  or Code.Stelem_I8
            or Code.Stelem_R4  or Code.Stelem_R8  or Code.Stelem;

        private static bool IsLdelem(Code op) => op
            is Code.Ldelem_Ref or Code.Ldelem_I   or Code.Ldelem_I1
            or Code.Ldelem_I2  or Code.Ldelem_I4  or Code.Ldelem_I8
            or Code.Ldelem_R4  or Code.Ldelem_R8  or Code.Ldelem_U1
            or Code.Ldelem_U2  or Code.Ldelem_U4  or Code.Ldelem;

        /// <summary>
        /// Symuluje efekt stosu dla instrukcji których nie śledzimy szczegółowo.
        /// Zapobiega "rozjechaniu" stosu które dawałoby błędne sloty przy kolejnych call.
        /// </summary>
        private static void ApplyGenericStackEffect(Code op, Stack<StackSlot> stack)
        {
            // pop 2, push 1 — arytmetyka binarna, porównania
            if (op is Code.Add    or Code.Sub    or Code.Mul    or Code.Div    or Code.Rem
                    or Code.And   or Code.Or     or Code.Xor    or Code.Shl    or Code.Shr
                    or Code.Shr_Un or Code.Ceq   or Code.Cgt    or Code.Cgt_Un
                    or Code.Clt   or Code.Clt_Un
                    or Code.Add_Ovf or Code.Add_Ovf_Un
                    or Code.Sub_Ovf or Code.Sub_Ovf_Un
                    or Code.Mul_Ovf or Code.Mul_Ovf_Un)
            {
                stack.TryPop(out _);
                stack.TryPop(out _);
                stack.Push(StackSlot.Unknown);
                return;
            }

            // pop 1, push 1 — konwersje, unarne, box/unbox, cast
            if (op is Code.Neg    or Code.Not
                    or Code.Conv_I  or Code.Conv_I1 or Code.Conv_I2  or Code.Conv_I4
                    or Code.Conv_I8 or Code.Conv_U  or Code.Conv_U1  or Code.Conv_U2
                    or Code.Conv_U4 or Code.Conv_U8 or Code.Conv_R4  or Code.Conv_R8
                    or Code.Conv_R_Un
                    or Code.Conv_Ovf_I  or Code.Conv_Ovf_I1 or Code.Conv_Ovf_I2
                    or Code.Conv_Ovf_I4 or Code.Conv_Ovf_I8
                    or Code.Conv_Ovf_U  or Code.Conv_Ovf_U1 or Code.Conv_Ovf_U2
                    or Code.Conv_Ovf_U4 or Code.Conv_Ovf_U8
                    or Code.Box   or Code.Unbox or Code.Unbox_Any
                    or Code.Castclass or Code.Isinst
                    or Code.Ldobj or Code.Ldind_I  or Code.Ldind_I1 or Code.Ldind_I2
                    or Code.Ldind_I4  or Code.Ldind_I8 or Code.Ldind_U1 or Code.Ldind_U2
                    or Code.Ldind_U4  or Code.Ldind_R4 or Code.Ldind_R8 or Code.Ldind_Ref
                    or Code.Ldlen     or Code.Ldvirtftn or Code.Ldftn)
            {
                stack.TryPop(out _);
                stack.Push(StackSlot.Unknown);
                return;
            }

            // pop 1, push 0 — store przez pointer, throw, endfilter
            if (op is Code.Stind_I  or Code.Stind_I1 or Code.Stind_I2 or Code.Stind_I4
                    or Code.Stind_I8 or Code.Stind_R4 or Code.Stind_R8 or Code.Stind_Ref
                    or Code.Stobj    or Code.Throw     or Code.Endfilter)
            {
                stack.TryPop(out _);
                return;
            }

            // push 0 — instrukcje bez efektu na stosie (nop, br, leave, itp.)
            // nic nie robimy
        }

        // ════════════════════════════════════════════════════════════════════════
        // Utils
        // ════════════════════════════════════════════════════════════════════════

        private static string MethodNodeId(string packageName, string methodFullName)
            => $"method:{packageName}:{methodFullName}";

        private static string ResolvePackage(IMethod m, MultiPackageResolver resolver)
        {
            var asm = m.DeclaringType?.DefinitionAssembly?.Name.String;
            return asm != null ? resolver.ResolvePackageName(asm) : "unknown";
        }

        private static int ParseArgIndex(string argName)
        {
            // "arg_0" → 0, "arg_12" → 12
            var span = argName.AsSpan("arg_".Length);
            return int.TryParse(span, out int idx) ? idx : -1;
        }

        private static DataFlowEdgeDto Edge(string from, string to, DataFlowEdgeType type)
            => new() { FromId = from, ToId = to, EdgeType = type };
    }
}