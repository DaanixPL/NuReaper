using App.Application.DTOs;

namespace NuReaper.Infrastructure.Repositories.GraphBuilders.Interfaces
{
    public interface IBuildRecursiveAsync
    {
        public Task Execute(
            string packageName,
            string packageVersion,
            string nuspecPath,
            string? targetFramework,
            DependencyGraphDto graph,
            HashSet<string> visited,
            Stack<string> currentPath,
            Dictionary<string, string> nodeIdMap,
            int depth,
            int maxDepth,
            CancellationToken cancellationToken);
    }
}
