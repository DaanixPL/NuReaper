using NuReaper.Application.DTOs;

namespace NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces
{
    public interface IScanModule
    {
        public Task<List<FindingSummaryDto>> Execute(string filePath, CancellationToken cancellationToken = default);
    }
}
