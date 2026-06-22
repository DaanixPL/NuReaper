using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using NuReaper.Application.Interfaces.DataFlow;
using NuReaper.Domain.Entities;
using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Infrastructure.Repositories.DataFlow;
using NuReaper.Infrastructure.Repositories.DataFlow.Analysis.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.Binders.Interface;
using NuReaper.Infrastructure.Repositories.DataFlow.Registries;

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
        private readonly IAnalyzeMethod _analyzeMethod;
        private readonly IBindParameters _bindParameters;
        public DataFlowGraphBuilder(ILogger<DataFlowGraphBuilder> logger, IAnalyzeMethod analyzeMethod, IBindParameters bindParameters)
        {
            _logger = logger;
            _analyzeMethod = analyzeMethod;
            _bindParameters = bindParameters;
        }

        // ════════════════════════════════════════════════════════════════════════
        // Public API
        // ════════════════════════════════════════════════════════════════════════

        public (DataFlowGraph, Dictionary<Package, int>) Build(
            IReadOnlyList<(string packageId, string dllPath)> inputs,
            List<Package> packages,
            CancellationToken cancellationToken = default)
        {
            var resolver  = new MultiPackageResolver();
            var moduleCtx = new ModuleContext(resolver);
            var loaded    = new List<(string packageKey, int packageId, ModuleDefMD module)>(inputs.Count);
            var deduplicatedInputs = inputs
                .GroupBy(x => x.dllPath, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            var allPackages = deduplicatedInputs
                .Select(x => x.packageId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var analyzedPackages = new Dictionary<int, string>(allPackages.Count);
            var packageIdsByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < allPackages.Count; i++)
            {
                analyzedPackages[i] = allPackages[i];
                packageIdsByKey[allPackages[i]] = i;
            }
            // for lookup in findings
            var packageToId = packages.ToDictionary(
                p => p,
                p => packageIdsByKey[p.NormalizedKey],
                EqualityComparer<Package>.Default);

            foreach (var (packageKey, path) in deduplicatedInputs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var module = ModuleDefMD.Load(path, moduleCtx);
                    if (!packageIdsByKey.TryGetValue(packageKey, out var packageId))
                        packageId = -1;

                    resolver.Register(module, packageId);
                    loaded.Add((packageKey, packageId, module));
                    _logger.LogDebug("Loaded '{Asm}' → '{Pkg}' ({PkgId})", module.Assembly?.Name.String, packageKey, packageId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Cannot load {Path} (package: {Pkg})", path, packageKey);
                }
            }

            // ── 2. Analiza IL ─────────────────────────────────────────────────────
            var nodes = new NodeRegistry();
            var edges = new List<DataFlowEdge>();

            foreach (var (packageKey, packageId, module) in loaded)
            {
                _logger.LogInformation("Analyzing package {Pkg} (ID: {PkgId})", packageKey, packageId);
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var type in module.GetTypes())
                {
                    foreach (var method in type.Methods)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (!method.HasBody || !method.Body.HasInstructions) continue;

                        _analyzeMethod.Execute(method, type, packageKey, packageId, module, resolver, nodes, edges);
                    }
                }
            }

            // ── 3. Post-processing: połącz argumenty z parametrami ────────────────
            _bindParameters.Execute(nodes, edges);
            for (int i = 0; i < edges.Count; i++)
                edges[i] = edges[i] with { Id = i };
            // ── 4. Cleanup ────────────────────────────────────────────────────────
            foreach (var (_, _, module) in loaded)
                module.Dispose();
            
            var dataFlowGraph = new DataFlowGraph
            {
                AnalyzedPackages = analyzedPackages,
                Nodes            = nodes.Nodes.ToList(),
                Edges            = edges,
                CreatedAt        = DateTime.UtcNow
            };
            return (dataFlowGraph, packageToId);
        }
    }
}