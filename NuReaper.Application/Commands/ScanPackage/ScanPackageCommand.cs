using MediatR;
using NuReaper.Application.Responses;

namespace NuReaper.Application.Commands.ScanPackage
{
    public record ScanPackageCommand(string url) : IRequest<ScanPackageResultResponse>;
}