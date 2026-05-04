using NuReaper.Domain.Entities.Graph;

namespace NuReaper.Infrastructure.Repositories.GraphBuilders.Interfaces
{
    public interface IBreadthFirstSearch
    {
        public Task<List<string>> Execute(DependencyGraph graph, string start, string target);
    }
}
