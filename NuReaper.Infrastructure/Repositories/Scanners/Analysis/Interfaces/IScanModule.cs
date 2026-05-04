using NuReaper.Domain.Entities;

namespace NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces
{
    public interface IScanModule
    {
        public Task<List<ScanFinding>> Execute(string filePath, CancellationToken cancellationToken = default);
    }
}
