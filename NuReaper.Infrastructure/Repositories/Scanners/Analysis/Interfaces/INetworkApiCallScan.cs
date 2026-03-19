
using NuReaper.Application.DTOs;

namespace NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces
{
    public interface INetworkApiCallScan
    {
        public Task<(List<FindingSummaryDto> Findings, string Sha256Hash)> Execute(
            string url,
            CancellationToken cancellationToken);
    }
}
