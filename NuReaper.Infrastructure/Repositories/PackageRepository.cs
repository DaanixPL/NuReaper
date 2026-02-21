using App.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using NuReaper.Domain.Abstractions;
using NuReaper.Domain.Entities;

namespace NuReaper.Infrastructure.Repositories
{
    public class PackageRepository : IPackageRepository
    {
        private readonly AppDbContext _context;
        public PackageRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddPackageAsync(Package package, CancellationToken cancellationToken = default)
        {
            await _context.Packages.AddAsync(package, cancellationToken);
        }
        public Task RemovePackageAsync(Package package, CancellationToken cancellationToken = default)
        {
            _context.Packages.Remove(package);
            return Task.CompletedTask;
        }
        public Task UpdatePackageAsync(Package package, CancellationToken cancellationToken = default)
        {
            _context.Packages.Update(package);
            return Task.CompletedTask;
        }

        public async Task<Package?> GetPackageByNormalizedKeyAsync(string normalizedKey, CancellationToken cancellationToken = default)
        {
            return await _context.Packages
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.NormalizedKey == normalizedKey, cancellationToken);
        }
    }
}