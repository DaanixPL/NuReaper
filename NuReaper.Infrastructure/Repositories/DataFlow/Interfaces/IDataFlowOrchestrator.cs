using NuReaper.Application.DTOs.DataFlow;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Interfaces
{
    public interface IDataFlowOrchestrator
    {
           Task<DataFlowGraphDto> BuildAsync(
            string rootPackageUrl,
            CancellationToken cancellationToken = default);
    }
}