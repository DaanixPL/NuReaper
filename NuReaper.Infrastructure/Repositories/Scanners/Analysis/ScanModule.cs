using System.Collections.Concurrent;
using dnlib.DotNet;
using Microsoft.Extensions.Logging;
using NuReaper.Application.DTOs;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Analysis
{
    public class ScanModule : IScanModule
    {
        private readonly IScanMethod _scanMethod;
        private readonly ILogger<ScanModule> _logger;

        public ScanModule(IScanMethod scanMethod, ILogger<ScanModule> logger)
        {
            _scanMethod = scanMethod;
            _logger = logger;
        }

        public async Task<List<FindingSummaryDto>> Execute(string filePath, CancellationToken cancellationToken)
        {
            var module = await Task.Run(() => ModuleDefMD.Load(filePath), cancellationToken).ConfigureAwait(false);

            try
            {
                var types = module.GetTypes();

                if (!types.Any())
                    return new List<FindingSummaryDto>();

                var estimatedMethodCount = types.Sum(t => t.Methods.Count);
                var findings = new ConcurrentBag<FindingSummaryDto>();

                await Task.Run(() =>
                {
                    Parallel.ForEach(types, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount, CancellationToken = cancellationToken }, type =>
                    {
                        foreach (var method in type.Methods)
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (!method.HasBody || !method.Body.HasInstructions)
                                continue;
                            
                            _logger.LogTrace("Scanning {TypeFullName}::{MethodName}...", type.FullName, method.Name);
                            var methodFindings = _scanMethod.Execute(method, type);
                            if (methodFindings.Count > 0)
                            {
                                foreach (var finding in methodFindings)                            
                                    findings.Add(finding);
                            }
                        }
                    });
                }, cancellationToken).ConfigureAwait(false);

                return findings.ToList();
            }
            finally
            {
                module?.Dispose();
            }
        }
    }
}
