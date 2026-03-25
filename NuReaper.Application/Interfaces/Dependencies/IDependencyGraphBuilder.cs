using NuReaper.Application.DTOs;

namespace NuReaper.Application.Interfaces.Dependencies
{
    public interface IDependencyGraphBuilder
    {
        Task<DependencyGraphDto> BuildGraphAsync(string url, int maxDepth, string? targetFramework, CancellationToken cancellationToken = default);
        Task<bool> HasCyclesAsync(string packageName, string version, string nuspecPath, CancellationToken cancellationToken = default);
        Task<List<string>> FindShortestPathsAsync(string fromPackage, string toPackage, string nuspecPath, CancellationToken cancellationToken = default);
    }
}
