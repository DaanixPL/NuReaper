using NuReaper.Application.Responses;

namespace NuReaper.Application.Interfaces.Jobs
{
    public interface IScanJobService
    {
        Task<ScanJobStatus?> GetScanJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default);
        Task<Guid> EnqueueJob(string url, CancellationToken cancellationToken = default);
    }
}