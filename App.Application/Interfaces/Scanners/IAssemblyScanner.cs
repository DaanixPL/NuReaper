using NuReaper.Application.Responses;

namespace NuReaper.Application.Interfaces.Scanners
{
    public interface IAssemblyScanner
    {
        Task<ScanPackageResultResponse> ScanPackageAsync(
            string url,
            CancellationToken cancellationToken);
    }
}