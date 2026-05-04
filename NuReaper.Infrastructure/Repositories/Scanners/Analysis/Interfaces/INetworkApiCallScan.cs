
using NuReaper.Domain.Entities;

namespace NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces
{
    public interface INetworkApiCallScan
    {
        public Task<(List<ScanFinding> Findings, string Sha256Hash)> Execute(
            string url,
            CancellationToken cancellationToken);
    }
}
