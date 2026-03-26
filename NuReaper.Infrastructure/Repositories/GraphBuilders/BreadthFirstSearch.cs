using NuReaper.Application.DTOs;
using NuReaper.Infrastructure.Repositories.GraphBuilders.Interfaces;

namespace NuReaper.Infrastructure.Repositories.GraphBuilders
{
    public class BreadthFirstSearch : IBreadthFirstSearch
    {
        public Task<List<string>> Execute(DependencyGraphDto graph, string start, string target)
        {
            var startNode = graph.Nodes.FirstOrDefault(n => $"{n.Name}@{n.Version}" == start);
            if (startNode == null) return Task.FromResult(new List<string>());

            var queue = new Queue<(string nodeId, List<string> path)>();
            var visited = new HashSet<string>();

            var nodesById = graph.Nodes.ToDictionary(n => n.Id);
            var edgesByFromId = graph.Edges
                .GroupBy(e => e.FromId)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ToId).ToList());


            queue.Enqueue((startNode.Id, new List<string> { start }));
            visited.Add(startNode.Id);

            while (queue.Count > 0)
            {
                var (currentId, path) = queue.Dequeue();
                
                if (!nodesById.TryGetValue(currentId, out var currentNode))
                    continue;
                
                var currentKey = $"{currentNode.Name}@{currentNode.Version}";
                if (currentKey == target) return Task.FromResult(path);

                if (!edgesByFromId.TryGetValue(currentId, out var neighbors))
                    continue;

                foreach (var nextId in neighbors)
                {
                   if (!visited.Add(nextId))
                        continue;

                    if (!nodesById.TryGetValue(nextId, out var nextNode))
                        continue;
                    
                    var newPath = new List<string>(path) { $"{nextNode.Name}@{nextNode.Version}" };
                    queue.Enqueue((nextId, newPath));
                }
            }

            return Task.FromResult(new List<string>());
        }
    }
}
