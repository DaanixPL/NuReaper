using App.Application.DTOs;
using App.Application.DTOs.Graph;
using Microsoft.Extensions.Logging;
using NuReaper.Infrastructure.Repositories.GraphBuilders.Conditions.Interfaces;

namespace NuReaper.Infrastructure.Repositories.GraphBuilders.Conditions
{
    public class Conditions : IConditions
    {
        private readonly ILogger<Conditions> _logger;
        public Conditions(ILogger<Conditions> logger)
        {
            _logger = logger;
        }
        public bool IsCycle(Stack<string> currentPath, string packageKey, DependencyGraphDto graph)
        {
            if (currentPath.Contains(packageKey))
            {
                graph.Cycles.Add(new CycleDto
                {
                    Path = currentPath.Reverse().Append(packageKey).ToList()
                });
                return true;
            }
            return false;
        }

        public bool IsMaxDepth(int depth, int maxDepth)
        {
            if (depth >= maxDepth)
            {
                _logger.LogTrace("{Indentation}⛔ Max depth", "".PadLeft(depth * 2));
                return true;
            }
            return false;
        }

        public bool IsVisited(HashSet<string> visited, string packageKey)
        {
            if (visited.Contains(packageKey))
            {
                _logger.LogTrace("{Indentation} Already visited", "".PadLeft(visited.Count * 2));
                return true;
            }
            return false;
        }
    }
}
