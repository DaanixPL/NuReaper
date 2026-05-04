using NuReaper.Domain.Entities;

namespace NuReaper.Application.Interfaces.Scanners
{
    public interface IAssemblyScanner
    {
        Task<ScanPackageResult> ScanPackageAsync(
            string url,
            CancellationToken cancellationToken);
    }
}