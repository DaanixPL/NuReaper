using App.Application.DTOs;

namespace App.Application.Interfaces.Dependencies
{
    public interface IDependencyRepository
    {
        Task<DependencyGraphDto> BuildGraphAsync(string rootPackageName, string rootPackageVersion, int maxDepth, string nuspecPath ,string? targetFramework, CancellationToken cancellationToken = default);
        Task<bool> HasCyclesAsync(string packageName, string version, string nuspecPath, CancellationToken cancellationToken = default);
        Task<List<string>> FindShortestPathsAsync(string fromPackage, string toPackage, string nuspecPath, CancellationToken cancellationToken = default);
    }
}
