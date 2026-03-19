using App.Application.DTOs;

namespace NuReaper.Infrastructure.Repositories.GraphBuilders.Conditions.Interfaces
{
    public interface IConditions
    {
        public bool IsCycle(Stack<string> currentPath, string packageKey, DependencyGraphDto graph);
        public bool IsMaxDepth(int depth, int maxDepth);
        public bool IsVisited(HashSet<string> visited, string packageKey);
    }
}
