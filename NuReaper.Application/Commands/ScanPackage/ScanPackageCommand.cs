using MediatR;

namespace NuReaper.Application.Commands.ScanPackage
{
    public record ScanPackageCommand(string url) : IRequest<Guid>;
}