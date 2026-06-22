using Microsoft.Extensions.Logging;
using NuReaper.Application.Interfaces.Scanners;
using NuReaper.Domain.Entities;
using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Infrastructure.Repositories.DataFlow.DataFlowPathBuilder.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.ApiCallRegistry;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces;
using System.Collections.Concurrent;

namespace NuReaper.Infrastructure.Repositories
{
    public class AssemblyScanner : IAssemblyScanner
    {   
        private static readonly HashSet<NuReaper.Domain.Enums.DataFlowEdgeType> StructuralEdges = new()
        {
            NuReaper.Domain.Enums.DataFlowEdgeType.Calls,
            NuReaper.Domain.Enums.DataFlowEdgeType.Targets,
            NuReaper.Domain.Enums.DataFlowEdgeType.Contains
        };

        private readonly IPatternDetector[] _patternDetectors;
        private readonly IDataFlowPathBuilder _dataFlowPathBuilder;
        private readonly ILogger<AssemblyScanner> _logger;

        public AssemblyScanner(IEnumerable<IPatternDetector> patternDetectors, IDataFlowPathBuilder dataFlowPathBuilder, ILogger<AssemblyScanner> logger)
        {
            _patternDetectors = patternDetectors.ToArray();
            _dataFlowPathBuilder = dataFlowPathBuilder;
            _logger = logger;
        }

        public Task<ScanPackageResult> ScanPackageAsync(string rootPackageName, string rootPackageVersion, DataFlowGraph graph, Dictionary<Package, int> packageToId, Guid jobId, CancellationToken cancellationToken)
        {
            var startTime = DateTime.UtcNow;
            var findings = new ConcurrentBag<ScanFinding>();
            
            ScanPackageResult resault = new ScanPackageResult
            {
                RootPackageName = rootPackageName,
                RootPackageVersion = rootPackageVersion,
                DependencyGraph = graph.DependencyGraph,
            };

            var now = DateTime.UtcNow;

            var incomingEdges = graph.Edges
                .Where(e => !StructuralEdges.Contains(e.EdgeType))
                .GroupBy(e => e.ToId)
                .ToDictionary(g => g.Key, g => g.Select(e => e.FromId).ToList());

            var nodesLookup = graph.Nodes.ToDictionary(n => n.Id);

            var visited = new HashSet<int>();

            cancellationToken.ThrowIfCancellationRequested();

            foreach (var pattern in _patternDetectors)
            {
                var patternFindings = pattern.Detect(graph, packageToId, incomingEdges, jobId);
                if (patternFindings != null && patternFindings.Count > 0)
                {
                    foreach (var finding in patternFindings)
                    {
                        findings.Add(finding);
                    }
                    break;
                }
            }
            List<Package> packages = new List<Package>();
            foreach (var package in packageToId.Keys)
            {
                var packageFindings = findings.Where(f => f.PackageId == package.Id).ToList();
                if (packageFindings.Count > 0)
                {
                    var scan = new Scan
                    {
                        PackageId = package.Id,
                        Version = package.Version,
                        Findings = packageFindings,
                        ThreatLevel = (float)packageFindings.Average(f => f.DangerLevel),
                    };
                    package.Scans.Add(scan);
                    packages.Add(package);
                }
            }
            resault.Packages = packages;
            float ScanTime = (float)(DateTime.UtcNow - startTime).TotalSeconds;
            resault.ScannedTimeAllPackages = ScanTime;
            // TODO: Tutaj czysczenie pakietow skanowanych
            return Task.FromResult(resault);
        }
    }
}
