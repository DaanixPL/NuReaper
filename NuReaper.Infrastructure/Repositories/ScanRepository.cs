using NuReaper.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;
using NuReaper.Domain.Abstractions;
using NuReaper.Domain.Entities;

namespace NuReaper.Infrastructure.Repositories
{
    public class ScanRepository : IScanRepository
    {
        private readonly AppDbContext _context;
        public ScanRepository(AppDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task AddScanAsync(Scan scan, CancellationToken cancellationToken = default)
        {
            var exists = await _context.Packages.AnyAsync(p => p.Id == scan.PackageId, cancellationToken);
                
            if (!exists)
            {
                throw new KeyNotFoundException($"Package with ID {scan.PackageId} not found"); // Dodaj castom exception
            }

            _context.Scans.Add(scan);
        }

        public async Task AddScansAsync(IEnumerable<Scan> scans, CancellationToken cancellationToken = default)
        {
            var scanList = scans.ToList();
            if (!scanList.Any())
                return; 

            var packageIds = scanList.Select(s => s.PackageId).Distinct().ToList();

            var existingPackageIds = await _context.Packages
                .Where(p => packageIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            var missingPackageIds = packageIds.Except(existingPackageIds).ToList();

            if (missingPackageIds.Any())
            {
                throw new KeyNotFoundException($"Packages with IDs {string.Join(", ", missingPackageIds)} not found"); // Dodaj castom exception
            }

            _context.Scans.AddRange(scanList);
        }

        public async Task RemoveScanAsync(Guid scanId, CancellationToken cancellationToken = default)
        {
            var rowsDeleted = await _context.Scans
                .Where(s => s.Id == scanId)
                .ExecuteDeleteAsync(cancellationToken);

            if (rowsDeleted == 0)
            {
                throw new KeyNotFoundException($"Scan with ID {scanId} not found"); // Dodaj castom exception
            }
        }
        public Task<Scan?> GetScanByIdAsync(string normalizedKey, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}