using Microsoft.Extensions.Logging;
using NuReaper.Domain.Entities;
using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.Scanners.ApiCallRegistry;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces;
using System.Text.RegularExpressions;

namespace NuReaper.Infrastructure.Repositories.Scanners.Detectors
{
    public class Pattern1_NetworkToExecution : IPatternDetector
    {
        private static readonly Regex UrlRegex = new(
            @"\b(?:https?|wss?)://[^\s\""""'<>]+",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private readonly ILogger<Pattern1_NetworkToExecution> _logger;

        public Pattern1_NetworkToExecution(
            ILogger<Pattern1_NetworkToExecution> logger)
        {
            _logger = logger;
        }

        public List<ScanFinding> Detect(DataFlowGraph graph, Dictionary<Package, int> packageToId, Dictionary<int, List<int>> incomingEdgesLookup, Guid scanId)
        {
            var findings = new List<ScanFinding>();
            var sinksId = graph.Nodes
                .Where(node => node.Type == DataFlowNodeType.CallSite)
                .Where(node => IsExecuteApiCall.Execute(node.TypeFullName))
                .Select(node => node.Id)
                .ToList();

            if (!sinksId.Any())
            {
                return findings;
            }

            foreach (int sinkId in sinksId)
            {
                var queue = new Queue<(int CurrentId, List<int> Path)>();
                var visited = new HashSet<int>();

                queue.Enqueue((sinkId, new List<int> { sinkId }));
                visited.Add(sinkId);

                while (queue.Count > 0)
                {
                    var (currentId, currentPath) = queue.Dequeue();

                    if (!graph.NodeLookup.TryGetValue(currentId, out var currentNode))
                        continue;

                    if (currentNode.Type == DataFlowNodeType.CallSite && IsNetworkApiCall.Execute(currentNode.TypeFullName))
                    {
                        currentPath.Reverse();
                        var traceNames = currentPath.Select(id => graph.NodeLookup[id].TypeFullName);
                        string fullFlowTrace = string.Join(" -> ", traceNames);

                        string transferredData = BuildTransferredDataDescription(graph, currentPath, incomingEdgesLookup, currentId);
                        string sourceLocation = BuildNodeLocation(graph, currentId);
                        string sinkLocation = BuildNodeLocation(graph, sinkId);

                        var pkg = packageToId.FirstOrDefault(x => x.Value == currentNode.PackageId).Key;
                        if (pkg is null) continue;

                        var finding = new ScanFinding
                        {
                            ScanId = scanId,
                            PackageId = pkg.Id,
                            Type = ScanFindingType.MalwareDownloader,
                            ConfidenceScore = 95.0f,
                            DangerLevel = 100.0f,
                            HopDepth = currentPath.Count - 1,
                            FlowTrace = fullFlowTrace,
                            Evidence = $"Data flow detected from network source [{currentNode.TypeFullName}] to execution sink [{graph.NodeLookup[sinkId].TypeFullName}]. Transferred data: {transferredData}",
                            Location = sinkLocation,
                            RawData = $"SinkID: {sinkId} | SourceID: {currentId}"
                        };

                        findings.Add(finding);
                        break;
                    }

                    if (incomingEdgesLookup.TryGetValue(currentId, out var parentIds))
                    {
                        foreach (int parentId in parentIds)
                        {
                            if (!visited.Contains(parentId))
                            {
                                visited.Add(parentId);
                                
                                var newPath = new List<int>(currentPath) { parentId };
                                queue.Enqueue((parentId, newPath));
                            }
                        }
                    }
                }
            }
            foreach (var finding in findings)
            {
                _logger.LogInformation("Pattern1_NetworkToExecution Finding: {Finding}", finding);
            }
            return findings;
        }

        private static string BuildTransferredDataDescription(
            DataFlowGraph graph,
            List<int> path,
            Dictionary<int, List<int>> incomingEdgesLookup,
            int sourceCallSiteId)
        {
            var nodes = path
                .Where(graph.NodeLookup.ContainsKey)
                .Select(id => graph.NodeLookup[id])
                .ToList();

            var literalCandidates = new List<string>();

            // 1) Literals directly on the found path
            literalCandidates.AddRange(nodes
                .Where(n => n.Type == DataFlowNodeType.Literal && !string.IsNullOrWhiteSpace(n.Name))
                .Select(n => n.Name.Trim()));

            // 2) Literals that flow into the source callsite (often URL/input arguments)
            if (incomingEdgesLookup.TryGetValue(sourceCallSiteId, out var parentIds))
            {
                foreach (var parentId in parentIds)
                {
                    if (!graph.NodeLookup.TryGetValue(parentId, out var parentNode))
                        continue;

                    if (parentNode.Type == DataFlowNodeType.Literal && !string.IsNullOrWhiteSpace(parentNode.Name))
                        literalCandidates.Add(parentNode.Name.Trim());
                }
            }

            // 3) Multi-hop upstream traversal (e.g. literal -> local var -> callsite arg)
            literalCandidates.AddRange(CollectUpstreamLiterals(
                graph,
                incomingEdgesLookup,
                sourceCallSiteId,
                maxDepth: 6,
                maxNodes: 256));

            var distinctLiterals = literalCandidates
                .Distinct(StringComparer.Ordinal)
                .Take(8)
                .ToList();

            var foundUrls = distinctLiterals
                .SelectMany(v => UrlRegex.Matches(v).Select(m => m.Value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (foundUrls.Count > 0)
                return $"URLs: {string.Join(", ", foundUrls.Select(u => $"'{u}'"))}";

            if (distinctLiterals.Count > 0)
                return $"literals: {string.Join(", ", distinctLiterals.Select(v => $"'{TrimForEvidence(v)}'"))}";

            return "unknown (no explicit literal value on path)";
        }

        private static IEnumerable<string> CollectUpstreamLiterals(
            DataFlowGraph graph,
            Dictionary<int, List<int>> incomingEdgesLookup,
            int startNodeId,
            int maxDepth,
            int maxNodes)
        {
            var result = new List<string>();
            var visited = new HashSet<int>();
            var queue = new Queue<(int NodeId, int Depth)>();

            queue.Enqueue((startNodeId, 0));
            visited.Add(startNodeId);

            int processed = 0;

            while (queue.Count > 0)
            {
                var (nodeId, depth) = queue.Dequeue();
                if (++processed > maxNodes)
                    break;

                if (depth >= maxDepth)
                    continue;

                if (!incomingEdgesLookup.TryGetValue(nodeId, out var parentIds))
                    continue;

                foreach (var parentId in parentIds)
                {
                    if (!visited.Add(parentId))
                        continue;

                    if (graph.NodeLookup.TryGetValue(parentId, out var parentNode) &&
                        parentNode.Type == DataFlowNodeType.Literal &&
                        !string.IsNullOrWhiteSpace(parentNode.Name))
                    {
                        result.Add(parentNode.Name.Trim());
                    }

                    queue.Enqueue((parentId, depth + 1));
                }
            }

            return result;
        }

        private static string BuildNodeLocation(DataFlowGraph graph, int nodeId)
        {
            if (!graph.NodeLookup.TryGetValue(nodeId, out var node))
                return $"nodeId={nodeId}";

            string ownerMethod = "<unknown method>";

            if (node.Type == DataFlowNodeType.Method)
            {
                ownerMethod = node.TypeFullName;
            }
            else if (node.ContainingMethodNodeId != 0 &&
                graph.NodeLookup.TryGetValue(node.ContainingMethodNodeId, out var methodNode))
            {
                ownerMethod = methodNode.TypeFullName;
            }

            string ilPart = node.InstructionOffset.HasValue
                ? $"IL_{node.InstructionOffset.Value:X4}"
                : "IL_<n/a>";

            return $"{ownerMethod} | nodeType={node.Type} | member={node.TypeFullName} | {ilPart}";
        }

        private static string TrimForEvidence(string value)
        {
            const int max = 140;
            if (string.IsNullOrEmpty(value) || value.Length <= max)
                return value;

            return value[..max] + "...";
        }

    }
}
