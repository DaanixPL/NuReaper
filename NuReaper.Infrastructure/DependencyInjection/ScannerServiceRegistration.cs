using App.Infrastructure.Repositories.Scanners.Finders;
using Microsoft.Extensions.DependencyInjection;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.ApiCallRegistry;
using NuReaper.Infrastructure.Repositories.Scanners.ApiCallRegistry.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors;
using NuReaper.Infrastructure.Repositories.Scanners.Detectors.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Finders;
using NuReaper.Infrastructure.Repositories.Scanners.Finders.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis;
using NuReaper.Infrastructure.Repositories.Scanners.InstructionAnalysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.Patterns;
using NuReaper.Infrastructure.Repositories.Scanners.Patterns.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation;
using NuReaper.Infrastructure.Repositories.Scanners.RiskCalculation.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis;
using NuReaper.Infrastructure.Repositories.Scanners.StringAnalysis.Interfaces;
using NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis;
using NuReaper.Infrastructure.Repositories.Scanners.VariableAnalysis.Interfaces;
using NuReaper.Infrastructure.Repositories.VariableAnalysis;

namespace NuReaper.Infrastructure.DependencyInjection
{
    public static class ScannerServiceRegistration
    {
        public static IServiceCollection AddScannerServices(this IServiceCollection services)
        {
            // Analysis
            services.AddScoped<IScanMethod, ScanMethod>();
            services.AddScoped<IScanModule, ScanModule>();

            // Api call registry
            services.AddScoped<IIsNetworkApiCall, IsNetworkApiCall>();

            // Detectors
            services.AddScoped<IPatternDetector, Pattern4_StringInterpolation>();
            services.AddScoped<IPatternDetector, Pattern1_DirectApiCallDetector>();
            services.AddScoped<IPatternDetector, Pattern2_StringAssigned>();
            services.AddScoped<IPatternDetector, Pattern3_StringConstruction>();
            services.AddScoped<IPatternDetector, Pattern5_CharOnlyInterpolation>();
            services.AddScoped<IPatternDetector, Pattern7_BareApiCalls>();
            services.AddScoped<IPatternDetector, Pattern6_DefaultInterpolatedStringHandler>();

            // Finders
            services.AddScoped<IFindNetworkApiCallAfterIndex, FindNetworkApiCallAfterIndex>();
            services.AddScoped<IExtractApiCallArguments, ExtractApiCallArguments>();
            services.AddScoped<IFindApiCallUsingVariable, FindApiCallUsingVariable>();
            services.AddScoped<IFindNetworkApiCall, FindNetworkApiCall>();
            services.AddScoped<IFindNextVariableStore, FindNextVariableStore>();
            services.AddScoped<IFindToStringAndClear, FindToStringAndClear>();

            // Finding creation
            services.AddScoped<ICreateFinding, CreateFinding>();
            services.AddScoped<IGetAssemblyFiles, GetAssemblyFiles>();
            services.AddScoped<IGetFindingType, GetFindingType>();
            services.AddScoped<IGetVariableName, GetVariableName>();

            // Instruction analysis
            services.AddScoped<IExtractVariableIndex, ExtractVariableIndex>();
            services.AddScoped<IIsCharacterCode, IsCharacterCode>();
            services.AddScoped<IIsStackChangingOperation, IsStackChangingOperation>();
            services.AddScoped<IIsVariableLoad, IsVariableLoad>();
            services.AddScoped<IIsVariableStore, IsVariableStore>();

            // Pattenrs
            services.AddScoped<IPatternRegistry, PatternRegistry>();

            // Risk Calculation
            services.AddScoped<ICalculateConfidenceScore, CalculateConfidenceScore>();
            services.AddScoped<ICalculateDangerLevel, CalculateDangerLevel>();

            // String analysis
            services.AddScoped<IReconstructFromInterpolatedHandler, ReconstructFromInterpolatedHandler>();
            services.AddScoped<IReconstructInterpolation, ReconstructInterpolation>();
            services.AddScoped<IReconstructStringFromChars, ReconstructStringFromChars>();

            // Variable Analysis
            services.AddScoped<ICollectStackArguments, CollectStackArguments>();
            services.AddScoped<IIsStoreToVariable, IsStoreToVariable>();
            services.AddScoped<ITryGetCharVariableValue, TryGetCharVariableValue>();
            services.AddScoped<ITryGetVariableValue, TryGetVariableValue>();
            services.AddScoped<IVariableIndexExtractor, VariableIndexExtractor>();

            return services;
        }
    }
}
