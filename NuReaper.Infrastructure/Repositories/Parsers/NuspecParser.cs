using App.Application.DTOs;
using NuReaper.Application.Interfaces.Parsers;
using NuReaper.Infrastructure.Repositories.Parsers.Strategies.Interfaces;
using System.Xml.Linq;

namespace NuReaper.Infrastructure.Repositories.Parsers
{
    public class NuspecParser : INuspecParser
    {
        private readonly IFrameworkAssemblyParser _frameworkAssemblyParser;
        private readonly INuGetDependencyParser _nuGetDependencyParser;
        public NuspecParser(IFrameworkAssemblyParser frameworkAssemblyParser, INuGetDependencyParser nuGetDependencyParser)
        {
            _frameworkAssemblyParser = frameworkAssemblyParser;
            _nuGetDependencyParser = nuGetDependencyParser;
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
                using var stream = File.OpenRead(nuspecFilePath);
                var xdoc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken);

                if (xdoc.Root == null)
                {
                    return dependencies;
                }

                var ns = xdoc.Root.GetDefaultNamespace();

                // Parse framework assemblies
                dependencies.AddRange(_frameworkAssemblyParser.ParseFrameworkAssemblies(xdoc, ns));

                // Parse NuGet dependencies
                dependencies.AddRange(_nuGetDependencyParser.ParseNuGetDependencies(xdoc, ns));

                return dependencies;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing nuspec file {nuspecFilePath}: {ex.Message}");
                throw new InvalidOperationException($"Failed to parse nuspec file: {nuspecFilePath}", ex);
            }
        }
    }
}