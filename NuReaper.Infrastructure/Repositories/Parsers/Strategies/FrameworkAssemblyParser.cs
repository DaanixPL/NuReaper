using System.Xml.Linq;
using App.Application.DTOs;
using NuReaper.Infrastructure.Repositories.Parsers.Strategies.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Parsers.Strategies
{
    public class FrameworkAssemblyParser : IFrameworkAssemblyParser
    {
        public List<DependencyDto> ParseFrameworkAssemblies(XDocument xdoc, XNamespace ns)
        {
            var dependencies = new List<DependencyDto>();

            var frameworkAssemblyElements = xdoc
                .Descendants(ns + "frameworkAssemblies")
                .Descendants(ns + "frameworkAssembly");

            foreach (var element in frameworkAssemblyElements)
            {
                var name = element.Attribute("assemblyName")?.Value;
                var targetFramework = element.Attribute("targetFramework")?.Value;

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                dependencies.Add(new DependencyDto
                {
                    Name = name,
                    Version = "Framework",
                    TargetFramework = targetFramework,
                    Type = "FrameworkAssembly",
                    IsTransitive = false
                });
            }

            return dependencies;
        }
    }
}
