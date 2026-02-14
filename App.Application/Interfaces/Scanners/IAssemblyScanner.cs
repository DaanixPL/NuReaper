using NuReaper.Application.Responses;

namespace NuReaper.Application.Interfaces.Scanners
{
    public interface IAssemblyScanner
    {
        Task<ScanPackageResultResponse> ScanPackageAsync(
            string packageName,
            string version,
            string sha256Hash,
            string extractedPath,
            CancellationToken cancellationToken);
    }
}