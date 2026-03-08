using App.Domain.Abstractions;
using App.Domain.Entities;
using App.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace NuReaper.Infrastructure.Repositories
{
    public class DependencyRepository : IDependencyRepository
    {
        private readonly AppDbContext _context;
        public DependencyRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }
        public async Task AddDependenciesAsync(Guid packageId, List<PackageDependency> dependencies, CancellationToken cancellationToken = default)
        {
            await _context.PackageDependencies.AddRangeAsync(dependencies, cancellationToken);
        }

        public async Task<List<PackageDependency>> GetDependenciesAsync(Guid packageId, CancellationToken cancellationToken = default)
        {
            return await _context.PackageDependencies.Where(d => d.PackageId == packageId).ToListAsync(cancellationToken);
        }
    }
}
