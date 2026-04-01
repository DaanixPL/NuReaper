using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuReaper.Application.Interfaces.Jobs;
using NuReaper.Application.Interfaces.Scanners;
using NuReaper.Application.Responses;

namespace NuReaper.Infrastructure.Repositories.Jobs
{
    public class ScanJobService : IScanJobService
    {
        private readonly ConcurrentDictionary<Guid, ScanJobStatus> _scanResults = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ScanJobService> _logger;

        public ScanJobService(ILogger<ScanJobService> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }
        public Task<ScanJobStatus?> GetScanJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            _scanResults.TryGetValue(jobId, out var status);
            return Task.FromResult(status);
        }

        public Task<Guid> EnqueueJob(string url, CancellationToken cancellationToken = default)
        {
            var jobId = Guid.NewGuid();
            _scanResults[jobId] = new ScanJobStatus { Status = "Pending" };

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var scanner = scope.ServiceProvider.GetRequiredService<IAssemblyScanner>();

                    var result = await scanner.ScanPackageAsync(url, CancellationToken.None);

                    var scanRes = _scanResults[jobId] = new ScanJobStatus
                    {
                        Status = "Completed",
                        Result = result
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing ScanPackageCommand for URL: {Url}", url);
                    var scanRes = _scanResults[jobId] = new ScanJobStatus
                    {
                         Status = "Failed",
                         ErrorMessage = ex.Message
                    };
                }
            });

            return Task.FromResult(jobId);
        }
    }
}
