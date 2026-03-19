using App.Application.DTOs;

namespace NuReaper.Infrastructure.Repositories.GraphBuilders.Interfaces
{
    public interface IBreadthFirstSearch
    {
        public Task<List<string>> Execute(DependencyGraphDto graph, string start, string target);
    }
}
