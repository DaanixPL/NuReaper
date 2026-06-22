using System.Reflection.Metadata;
using Microsoft.Extensions.DependencyInjection;
using NuReaper.Application.Interfaces.DataFlow;
using NuReaper.Infrastructure.Repositories.DataFlow;
using NuReaper.Infrastructure.Repositories.DataFlow.Analysis;
using NuReaper.Infrastructure.Repositories.DataFlow.Analysis.InstructionAnalysis;
using NuReaper.Infrastructure.Repositories.DataFlow.Analysis.InstructionAnalysis.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.Analysis.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.Binders;
using NuReaper.Infrastructure.Repositories.DataFlow.Binders.Interface;
using NuReaper.Infrastructure.Repositories.DataFlow.DataFlowPathBuilder;
using NuReaper.Infrastructure.Repositories.DataFlow.DataFlowPathBuilder.DFS;
using NuReaper.Infrastructure.Repositories.DataFlow.DataFlowPathBuilder.DFS.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.DataFlowPathBuilder.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.Ensures;
using NuReaper.Infrastructure.Repositories.DataFlow.Ensures.Interface;
using NuReaper.Infrastructure.Repositories.DataFlow.Handlers;
using NuReaper.Infrastructure.Repositories.DataFlow.Handlers.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlow.Processes;
using NuReaper.Infrastructure.Repositories.DataFlow.Processes.Interfaces;
using NuReaper.Infrastructure.Repositories.DataFlowAnalysis;

namespace NuReaper.Infrastructure.DependencyInjection
{
    public static class DataFlowServiceRegistration
    {
          public static IServiceCollection AddDataFlowServices(this IServiceCollection services)
        {
            services.AddScoped<IDataFlowGraphBuilder, DataFlowGraphBuilder>();
            services.AddScoped<IDataFlowOrchestrator, DataFlowOrchestrator>();
            services.AddScoped<IProcessInstruction, ProcessInstruction>();

            // Analysis
            services.AddScoped<IAnalyzeMethod, AnalyzeMethod>();

            // Binders
            services.AddScoped<IBindParameters, BindParameters>();

            // Ensures
            services.AddScoped<IEnsuring, Ensuring>();

            // Handlers
            services.AddScoped<IHandleCall, HandleCall>();

            // Helper
            services.AddScoped<IOpcodeHelpers, OpcodeHelpers>();

            // DataFlowPathBuilder
            services.AddScoped<IDataFlowPathBuilder, DataFlowPathBuilder>();

            // DFS
            services.AddScoped<IDFS, DFS>();
            return services;
        }
    }
}