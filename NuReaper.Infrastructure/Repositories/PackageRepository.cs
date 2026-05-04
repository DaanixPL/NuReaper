using NuReaper.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using NuReaper.Domain.Abstractions;
using NuReaper.Domain.Entities;

namespace NuReaper.Infrastructure.Repositories
{
    public class PackageRepository : IPackageRepository
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        public PackageRepository(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        public async Task AddPackageAsync(Package package, CancellationToken cancellationToken = default)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            await _context.Packages.AddAsync(package, cancellationToken);
        }
        public async Task RemovePackageAsync(Package package, CancellationToken cancellationToken = default)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            _context.Packages.Remove(package);
        }
        public async Task UpdatePackageAsync(Package package, CancellationToken cancellationToken = default)
        {
            await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            _context.Packages.Update(package);
        }

        public async Task<Package?> GetPackageByNormalizedKeyAsync(string normalizedKey, CancellationToken cancellationToken = default)
        {
            var idx = normalizedKey.LastIndexOf('@');
            if (idx <= 0)
                throw new ArgumentException($"Invalid normalizedKey: '{normalizedKey}'. Expected 'Name@Version'.");
            var name = normalizedKey.Substring(0, idx);
            var version = normalizedKey.Substring(idx + 1);
            await using var _context = await _contextFactory.CreateDbContextAsync(cancellationToken);
            return await _context.Packages
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PackageName == name && p.Version == version, cancellationToken);
        }

        public async Task<List<Package>> GetPackagesByNormalizedKeyAsync(IEnumerable<string> normalizedKeys, CancellationToken cancellationToken = default)
        {
            return await _context.Packages
                .AsNoTracking()
                .Include(p => p.Scans)
                .Include(p => p.Dependencies)
                .Where(p => normalizedKeys.Contains(p.NormalizedKey))
                .ToListAsync(cancellationToken);
        }
    }
}