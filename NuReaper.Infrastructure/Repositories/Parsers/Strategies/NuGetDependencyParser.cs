using System.Xml.Linq;
using App.Application.DTOs;
using NuReaper.Infrastructure.Repositories.Parsers.Strategies.Interfaces;
using NuReaper.Infrastructure.Repositories.Parsers.Utils.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Parsers.Strategies
{
    public class NuGetDependencyParser : INuGetDependencyParser
    {
        private readonly IParseDependencyElement _parseDependencyElement;
        public NuGetDependencyParser(IParseDependencyElement parseDependencyElement)
        {
            _parseDependencyElement = parseDependencyElement;
        }
        public List<DependencyDto> ParseNuGetDependencies(XDocument xdoc, XNamespace ns)
        {
            var dependencies = new List<DependencyDto>();

            var dependenciesElement = xdoc.Descendants(ns + "dependencies").FirstOrDefault();
            if (dependenciesElement == null)
                return dependencies;

            var groups = dependenciesElement.Elements(ns + "group").ToList();

            if (groups.Any())
            {
                foreach (var group in groups)
                {
                    var targetFramework = group.Attribute("targetFramework")?.Value;

                    var dependencyElements = group.Elements(ns + "dependency");

                    foreach (var dep in dependencyElements)
                    {
                        var dependency = _parseDependencyElement.Execute(dep, targetFramework);
                        if (dependency != null)
                        {
                            dependencies.Add(dependency);
                        }
                    }
                }
            }
            else
            {
                var dependencyElements = dependenciesElement.Elements(ns + "dependency");

                foreach (var dep in dependencyElements)
                {
                    var dependency = _parseDependencyElement.Execute(dep, targetFramework: null);
                    if (dependency != null)
                    {
                        dependencies.Add(dependency);
                    }
                }
            }

            return dependencies;
        }
    }
}
