using App.Application.DTOs;
using Microsoft.Extensions.Logging;
using NuReaper.Application.Interfaces.Parsers;
using NuReaper.Infrastructure.Repositories.Parsers.Strategies.Interfaces;
using System.Xml.Linq;

namespace NuReaper.Infrastructure.Repositories.Parsers
{
    public class NuspecParser : INuspecParser
    {
        private readonly IFrameworkAssemblyParser _frameworkAssemblyParser;
        private readonly INuGetDependencyParser _nuGetDependencyParser;
        private readonly ILogger<NuspecParser> _logger;
        public NuspecParser(IFrameworkAssemblyParser frameworkAssemblyParser, INuGetDependencyParser nuGetDependencyParser, ILogger<NuspecParser> logger)
        {
            _frameworkAssemblyParser = frameworkAssemblyParser;
            _nuGetDependencyParser = nuGetDependencyParser;
            _logger = logger;
        }

        public async Task<List<DependencyDto>> ParseDependenciesAsync(
            string nuspecFilePath, 
            CancellationToken cancellationToken = default)
        {
            if (!File.Exists(nuspecFilePath))
            {
                throw new FileNotFoundException($"Nuspec file not found: {nuspecFilePath}");
            }

            var dependencies = new List<DependencyDto>();

            try
            {
                _logger.LogTrace("Parsing nuspec file: {NuspecFilePath}", nuspecFilePath);
                using var stream = File.OpenRead(nuspecFilePath);
                var xdoc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

                if (xdoc.Root == null)
                {
                    _logger.LogWarning("Nuspec file {NuspecFilePath} has no root element.", nuspecFilePath);
                    return dependencies;
                }

                var ns = xdoc.Root.GetDefaultNamespace();

                // Parse framework assemblies
                dependencies.AddRange(_frameworkAssemblyParser.ParseFrameworkAssemblies(xdoc, ns));

                // Parse NuGet dependencies
                dependencies.AddRange(_nuGetDependencyParser.ParseNuGetDependencies(xdoc, ns));

                _logger.LogTrace("Parsed {DependencyCount} dependencies from nuspec file {NuspecFilePath}", dependencies.Count, nuspecFilePath);
                return dependencies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing nuspec file {NuspecFilePath}", nuspecFilePath);
                throw new InvalidOperationException($"Failed to parse nuspec file: {nuspecFilePath}", ex);
            }
        }
    }
}