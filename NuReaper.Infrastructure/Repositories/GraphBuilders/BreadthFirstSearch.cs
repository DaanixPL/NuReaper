using App.Application.DTOs;
using NuReaper.Infrastructure.Repositories.GraphBuilders.Interfaces;

namespace NuReaper.Infrastructure.Repositories.GraphBuilders
{
    public class BreadthFirstSearch : IBreadthFirstSearch
    {
        public async Task<List<string>> Execute(DependencyGraphDto graph, string start, string target)
        {
            var queue = new Queue<(string nodeId, List<string> path)>();
            var visited = new HashSet<string>();

            var startNode = graph.Nodes.FirstOrDefault(n => $"{n.Name}@{n.Version}" == start);
            if (startNode == null) return new List<string>();

            queue.Enqueue((startNode.Id, new List<string> { start }));
            visited.Add(startNode.Id);

            while (queue.Count > 0)
            {
                var (currentId, path) = queue.Dequeue();
                var currentNode = graph.Nodes.First(n => n.Id == currentId);
                var currentKey = $"{currentNode.Name}@{currentNode.Version}";

                if (currentKey == target) return path;

                foreach (var edge in graph.Edges.Where(e => e.FromId == currentId))
                {
                    if (!visited.Contains(edge.ToId))
                    {
                        visited.Add(edge.ToId);
                        var nextNode = graph.Nodes.FirstOrDefault(n => n.Id == edge.ToId);
                        
                        if (nextNode != null)
                        {
                            var newPath = new List<string>(path) { $"{nextNode.Name}@{nextNode.Version}" };
                            queue.Enqueue((edge.ToId, newPath));
                        }
                    }
                }
            }

            return new List<string>();
        }
    }
}
