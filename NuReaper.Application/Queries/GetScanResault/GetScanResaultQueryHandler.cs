using MediatR;
using NuReaper.Application.Interfaces.Jobs;
using NuReaper.Application.Responses;
using NuReaper.Application.Validators.Exceptions;

namespace NuReaper.Application.Queries.GetScanResult
{
    public class GetScanResultQueryHandler : IRequestHandler<GetScanResultQuery, ScanJobStatus?>
    {
        private readonly IScanJobService _scanJobService;

        public GetScanResultQueryHandler(IScanJobService scanJobService)
        {
            _scanJobService = scanJobService;
        }

        public async Task<ScanJobStatus?> Handle(GetScanResultQuery request, CancellationToken cancellationToken)
        {
            var result = await _scanJobService.GetScanJobStatusAsync(request.JobId, cancellationToken);
            if (result == null)
                throw new NotFoundException($"Scan job", request.JobId.ToString());
            return result;
        }
    }
}