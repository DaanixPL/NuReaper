using App.Infrastructure.Context;
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
            var package = await _context.Packages
                .Include(p => p.Scans)
                .FirstOrDefaultAsync(p => p.Id == scan.PackageId, cancellationToken);

            if (package == null)
            {
                throw new KeyNotFoundException($"Package with ID {scan.PackageId} not found"); // Dodaj castom exception
            }

            package.Scans.Add(scan);
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
    }
}