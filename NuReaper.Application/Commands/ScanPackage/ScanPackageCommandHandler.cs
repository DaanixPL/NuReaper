using System.IO.Compression;
using System.Security.Cryptography;
using NuReaper.Application.Interfaces.Dependencies;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NuReaper.Application.DTOs;
using NuReaper.Application.Interfaces.Parsers;
using NuReaper.Application.Interfaces.Scanners;
using NuReaper.Application.Responses;

namespace NuReaper.Application.Commands.ScanPackage
{
    public class ScanPackageCommandHandler : IRequestHandler<ScanPackageCommand, ScanPackageResultResponse>
    {
        private readonly IHostEnvironment _env;
        private readonly IAssemblyScanner _scanner;
        private readonly ILogger<ScanPackageCommandHandler> _logger;

        public ScanPackageCommandHandler(IAssemblyScanner scanner, IHostEnvironment env, ILogger<ScanPackageCommandHandler> logger)
        {
            _scanner = scanner;
            _env = env;
            _logger = logger;
        }

        public async Task<ScanPackageResultResponse> Handle(ScanPackageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                string urlToDownload = request.url.Replace("nuget.org/packages", "nuget.org/api/v2/package");

                if (_env.IsProduction())
                {
                    if (!urlToDownload.StartsWith("https://www.nuget.org/api/v2/package/"))
                    {
                        throw new ArgumentException("Invalid URL format. Expected format: https://www.nuget.org/packages/{packageId}/{version}");
                    }
                }
                
                var result = await _scanner.ScanPackageAsync(urlToDownload, cancellationToken);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing ScanPackageCommand for URL: {Url}", request.url);
                throw;
            }
        }
    }
}