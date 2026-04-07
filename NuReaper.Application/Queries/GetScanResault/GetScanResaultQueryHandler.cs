using AutoMapper;
using MediatR;
using NuReaper.Application.Interfaces.Jobs;
using NuReaper.Application.Responses;
using NuReaper.Application.Validators.Exceptions;
using NuReaper.Domain.Abstractions;
using NuReaper.Domain.Entities;

namespace NuReaper.Application.Queries.GetScanResult
{
    public class GetScanResultQueryHandler : IRequestHandler<GetScanResultQuery, ScanJobStatus?>
    {
        private readonly IScanJobService _scanJobService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetScanResultQueryHandler(IScanJobService scanJobService, IUnitOfWork unitOfWork, IMapper mapper)
        {
            _scanJobService = scanJobService;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<ScanJobStatus?> Handle(GetScanResultQuery request, CancellationToken cancellationToken)
        {
            var resultJobService = await _scanJobService.GetScanJobStatusAsync(request.JobId, cancellationToken);
            if (resultJobService == null)
                throw new NotFoundException($"Scan job", request.JobId.ToString());
            if (resultJobService.Result == null)
                throw new NotFoundException($"Scan job result", request.JobId.ToString());
            
            var packages = _mapper.Map<List<Package>>(resultJobService.Result.Packages);

            var existingPackages = await _unitOfWork.PackageRepository.GetPackagesByNormalizedKeyAsync(packages.Select(p => p.NormalizedKey).ToList(), cancellationToken);

            var packagesToAdd = packages.Except(existingPackages).ToList();

            await _unitOfWork.PackageRepository.AddPackagesAsync(packagesToAdd, cancellationToken);

            await _unitOfWork.ScanRepository.AddScansAsync(packagesToAdd.SelectMany(p => p.Scans), cancellationToken);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return resultJobService;
        }
    }
}