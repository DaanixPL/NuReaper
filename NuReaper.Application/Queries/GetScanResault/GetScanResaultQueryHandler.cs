using AutoMapper;
using MediatR;
using NuReaper.Application.Interfaces.Jobs;
using NuReaper.Application.Responses;
using NuReaper.Application.Validators.Exceptions;

namespace NuReaper.Application.Queries.GetScanResult
{
    public class GetScanResultQueryHandler : IRequestHandler<GetScanResultQuery, ScanJobStatusResponse?>
    {
        private readonly IScanJobService _scanJobService;
        private readonly IMapper _mapper;

        public GetScanResultQueryHandler(IScanJobService scanJobService, IMapper mapper)
        {
            _scanJobService = scanJobService;
            _mapper = mapper;
        }

        public async Task<ScanJobStatusResponse?> Handle(GetScanResultQuery request, CancellationToken cancellationToken)
        {
            var result = await _scanJobService.GetScanJobStatusAsync(request.JobId, cancellationToken);
            if (result == null)
                throw new NotFoundException($"Scan job", request.JobId.ToString());
            var scanJobStatus = _mapper.Map<ScanJobStatusResponse>(result);
            return scanJobStatus;
        }
    }
}