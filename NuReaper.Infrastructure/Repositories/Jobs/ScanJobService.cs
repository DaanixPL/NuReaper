using System.Collections.Concurrent;
using System.IO.Compression;
using System.Xml.Linq;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NuReaper.Domain.Entities;
using NuReaper.Domain.Entities.DataFlow;
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

                    DataFlowGraph graph;
                    Dictionary<Package, int> packageToId;
                    string rootPackageName;
                    string rootPackageVersion;

                    if (TryGetLocalNupkgPath(url, out var localNupkgPath))
                    {
                        (rootPackageName, rootPackageVersion) = ReadIdentityFromNupkg(localNupkgPath);
                        (graph, packageToId) = await dataFlowOrchestrator.BuildLocalNupkgAsync(
                            localNupkgPath,
                            rootPackageName,
                            rootPackageVersion,
                            cancellationToken);
                    }
                    else
                    {
                        (rootPackageName, rootPackageVersion) = ParseNugetIdentity(url);
                        (graph, packageToId) = await dataFlowOrchestrator.BuildAsync(
                            rootPackageName,
                            rootPackageVersion,
                            cancellationToken);
                    }

                    var scanResult = await scanner.ScanPackageAsync(
                        rootPackageName,
                        rootPackageVersion,
                        graph,
                        packageToId,
                        jobId,
                        cancellationToken);

                    var result = _mapper.Map<ScanPackageResultResponse>(scanResult);

                    var options = new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                    };
                    var json = System.Text.Json.JsonSerializer.Serialize((graph, packageToId), options);
                    var path = Path.Combine(Path.GetTempPath(), $"NuReaperGraph_{jobId}.json");
                    await File.WriteAllTextAsync(path, json, cancellationToken);
                    var scanRes = _scanResults[jobId] = new ScanJobStatusResponse
                    {
                        Status = "Completed",
                        Result = result,
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

        private static bool TryGetLocalNupkgPath(string input, out string localPath)
        {
            localPath = string.Empty;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (input.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                if (!Uri.TryCreate(input, UriKind.Absolute, out var fileUri) || !fileUri.IsFile)
                    return false;

                localPath = fileUri.LocalPath;
                localPath = Path.GetFullPath(localPath);
                return localPath.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase);
            }

            if (Uri.TryCreate(input, UriKind.Absolute, out var uri) && uri.IsFile)
            {
                localPath = Path.GetFullPath(uri.LocalPath);
                return localPath.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase);
            }

            localPath = Path.GetFullPath(input);
            return localPath.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase);
        }

        private static (string Name, string Version) ParseNugetIdentity(string input)
        {
            if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
                throw new ArgumentException("Invalid package URL.");

            var parts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var idx = Array.FindIndex(parts, p =>
                p.Equals("package", StringComparison.OrdinalIgnoreCase)
                || p.Equals("packages", StringComparison.OrdinalIgnoreCase));

            if (idx < 0 || parts.Length <= idx + 2)
                throw new ArgumentException("Invalid URL format. Expected .../package/{id}/{version}");

            return (parts[idx + 1], parts[idx + 2]);
        }

        private static (string Name, string Version) ReadIdentityFromNupkg(string nupkgPath)
        {
            using var archive = ZipFile.OpenRead(nupkgPath);
            var nuspecEntry = archive.Entries.FirstOrDefault(e =>
                e.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));

            if (nuspecEntry is null)
                throw new ArgumentException("No .nuspec found in local .nupkg.");

            using var stream = nuspecEntry.Open();
            var doc = XDocument.Load(stream);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
            var metadata = doc.Root?.Element(ns + "metadata");

            var id = metadata?.Element(ns + "id")?.Value?.Trim();
            var version = metadata?.Element(ns + "version")?.Value?.Trim();

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("Invalid .nuspec metadata (id/version).");

            return (id, version);
        }
    }
}
