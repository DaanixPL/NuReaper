using App.Domain.Entities;

namespace App.Domain.Abstractions
{
    public interface IDependencyRepository
    {
        Task AddDependenciesAsync(Guid packageId, List<PackageDependency> dependencies, CancellationToken cancellationToken = default);
        Task<List<PackageDependency>> GetDependenciesAsync(Guid packageId, CancellationToken cancellationToken = default);
    }
}
