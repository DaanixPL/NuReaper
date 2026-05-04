using NuReaper.Domain.Entities;

namespace NuReaper.Domain.Abstractions
{
    public interface IScanRepository
    {
        Task AddScanAsync(Scan scan, CancellationToken cancellationToken = default);
        Task AddScansAsync(IEnumerable<Scan> scans, CancellationToken cancellationToken = default);
        Task RemoveScanAsync(Guid scanId, CancellationToken cancellationToken = default);
        Task<Scan?> GetScanByIdAsync(string normalizedKey, CancellationToken cancellationToken = default);
    }
}