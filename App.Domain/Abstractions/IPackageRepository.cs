using NuReaper.Domain.Entities;

namespace NuReaper.Domain.Abstractions
{
    public interface IPackageRepository
    {
        Task AddPackageAsync(Package package, CancellationToken cancellationToken = default);
        Task RemovePackageAsync(Package package, CancellationToken cancellationToken = default);
        Task UpdatePackageAsync(Package package, CancellationToken cancellationToken = default);
        
        Task<Package?> GetPackageByNormalizedKeyAsync(string normalizedKey, CancellationToken cancellationToken = default);
    }
}