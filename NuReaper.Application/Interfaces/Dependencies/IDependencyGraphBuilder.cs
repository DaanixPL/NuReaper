using NuReaper.Domain.Entities.Graph;

namespace NuReaper.Application.Interfaces.Dependencies
{
    public interface IDependencyGraphBuilder
    {
        Task<DependencyGraph> BuildGraphAsync(string rootPackageName, string rootPackageVersion, int maxDepth, string? targetFramework, CancellationToken cancellationToken = default);
        Task<bool> HasCyclesAsync(string packageName, string version, string nuspecPath, CancellationToken cancellationToken = default);
        Task<List<string>> FindShortestPathsAsync(string fromPackage, string toPackage, string nuspecPath, CancellationToken cancellationToken = default);
    }
}
