using NuReaper.Domain.Entities.DataFlow;
using NuReaper.Infrastructure.Repositories.DataFlow.Registries;

namespace NuReaper.Infrastructure.Repositories.DataFlow.Binders.Interface
{
    public interface IBindParameters
    {
        public void Execute(
            NodeRegistry nodeRegistry,
            List<DataFlowEdge> edges);
    }
}
