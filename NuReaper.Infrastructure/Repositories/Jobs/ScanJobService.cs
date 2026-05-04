using System.Collections.Concurrent;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuReaper.Application.Interfaces.Jobs;
using NuReaper.Application.Interfaces.Scanners;
using NuReaper.Application.Responses;
using NuReaper.Infrastructure.Repositories.DataFlow.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Jobs
{
    public class ScanJobService : IScanJobService
    {
        private readonly ConcurrentDictionary<Guid, ScanJobStatusResponse> _scanResults = new();
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ScanJobService> _logger;
        private readonly IMapper _mapper;

        public ScanJobService(ILogger<ScanJobService> logger, IServiceScopeFactory scopeFactory, IMapper mapper)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _mapper = mapper;
        }
        public Task<ScanJobStatusResponse?> GetScanJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            _scanResults.TryGetValue(jobId, out var status);
            return Task.FromResult(status);
        }

        public Task<Guid> EnqueueJob(string url, CancellationToken cancellationToken = default)
        {
            var jobId = Guid.NewGuid();
            _scanResults[jobId] = new ScanJobStatusResponse { Status = "Pending" };

            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var scanner = scope.ServiceProvider.GetRequiredService<IAssemblyScanner>();
                    var dataFlowOrchestrator = scope.ServiceProvider.GetRequiredService<IDataFlowOrchestrator>();

                    // var scanTask = scanner.ScanPackageAsync(url, CancellationToken.None);
                    // var resultResponse = _mapper.Map<ScanPackageResultResponse>(scanTask.Result);

                    var dataFlowTask = dataFlowOrchestrator.BuildAsync(url, CancellationToken.None);

                    await Task.WhenAll(dataFlowTask);

                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                    };
                    var json = System.Text.Json.JsonSerializer.Serialize(dataFlowTask.Result, options);
                    var path = Path.Combine(Path.GetTempPath(), $"NuReaperGraph_{jobId}.json");
                    await File.WriteAllTextAsync(path, json);

                    var scanRes = _scanResults[jobId] = new ScanJobStatusResponse
                    {
                        Status = "Completed",
                        Result = null, // Debug
                        DataFlowGraphId = jobId.ToString()
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing ScanPackageCommand for URL: {Url}", url);
                    var scanRes = _scanResults[jobId] = new ScanJobStatusResponse
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
