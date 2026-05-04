using Microsoft.Extensions.DependencyInjection;
using NuReaper.Application.Interfaces.DataFlow;
using NuReaper.Infrastructure.Repositories.DataFlow;
using NuReaper.Infrastructure.Repositories.DataFlow.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlowAnalysis;

namespace NuReaper.Infrastructure.DependencyInjection
{
    public static class DataFlowServiceRegistration
    {
          public static IServiceCollection AddDataFlowServices(this IServiceCollection services)
        {
            services.AddScoped<IDataFlowGraphBuilder, DataFlowGraphBuilder>();
            services.AddScoped<IDataFlowOrchestrator, DataFlowOrchestrator>();
            
            return services;
        }
    }
}