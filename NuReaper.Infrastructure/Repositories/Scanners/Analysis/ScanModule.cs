using dnlib.DotNet;
using NuReaper.Application.DTOs;
using NuReaper.Infrastructure.Repositories.Scanners.Analysis.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Analysis
{
    public class ScanModule : IScanModule
    {
        private readonly IScanMethod _scanMethod;

        public ScanModule(IScanMethod scanMethod)
        {
            _scanMethod = scanMethod;
        }

        public List<FindingSummaryDto> Execute(string filePath)
        {
            var findings = new List<FindingSummaryDto>();
            ModuleDefMD? module = null;

            try
            {
                module = ModuleDefMD.Load(filePath);

                foreach (var type in module.GetTypes())
                {
                    foreach (var method in type.Methods)
                    {
                        if (!method.HasBody || !method.Body.HasInstructions)
                            continue;
                            
                        Console.WriteLine($"Scanning {type.FullName}::{method.Name}...");
                        var methodFindings = _scanMethod.Execute(method, type);
                        findings.AddRange(methodFindings);
                    }
                }
            }
            finally
            {
                module?.Dispose();
            }

            return findings;
        }
    }
}
