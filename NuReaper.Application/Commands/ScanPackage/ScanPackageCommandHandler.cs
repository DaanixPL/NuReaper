using AutoMapper;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NuReaper.Application.Interfaces.Jobs;

namespace NuReaper.Application.Commands.ScanPackage
{
    public class ScanPackageCommandHandler : IRequestHandler<ScanPackageCommand, Guid>
    {
        private readonly IScanJobService _scanJobService;
        private readonly IHostEnvironment _env;
        private readonly ILogger<ScanPackageCommandHandler> _logger;
        public ScanPackageCommandHandler(IHostEnvironment env, ILogger<ScanPackageCommandHandler> logger, IScanJobService scanJobService)
        {
            _env = env;
            _logger = logger;
            _scanJobService = scanJobService;
        }

        public async Task<Guid> Handle(ScanPackageCommand request, CancellationToken cancellationToken)
        {
            string urlToDownload = request.url.Replace("nuget.org/packages", "nuget.org/api/v2/package");

            if (_env.IsProduction())
            {
                if (!urlToDownload.StartsWith("https://www.nuget.org/api/v2/package/"))
                {
                    _logger.LogWarning("Invalid URL received in production environment: {Url}", request.url);
                    throw new ArgumentException("Invalid URL. Only packages from nuget.org are allowed.");
                }
            }

            var jobId = await _scanJobService.EnqueueJob(urlToDownload, cancellationToken);
            return jobId;
        }

    }

}