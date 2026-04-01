using MediatR;
using NuReaper.Application.Responses;

namespace NuReaper.Application.Queries.GetScanResult
{
    public record GetScanResultQuery(Guid JobId) : IRequest<ScanJobStatus?>;
}