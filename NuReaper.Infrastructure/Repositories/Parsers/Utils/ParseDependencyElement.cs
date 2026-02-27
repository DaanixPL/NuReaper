using System.Xml.Linq;
using App.Application.DTOs;
using NuReaper.Infrastructure.Repositories.Parsers.Utils.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Parsers.Utils
{
    public class ParseDependencyElement : IParseDependencyElement
    {
        private readonly IParseVersionRange _parseVersionRange;
        public ParseDependencyElement(IParseVersionRange parseVersionRange)
        {            
            _parseVersionRange = parseVersionRange;
        }
        public DependencyDto? Execute(XElement element, string? targetFramework)
        {
            var id = element.Attribute("id")?.Value;
            var version = element.Attribute("version")?.Value;

            if (string.IsNullOrWhiteSpace(id))
                return null;

            var versionRange = _parseVersionRange.Execute(version);

            return new DependencyDto
            {
                Name = id,
                Version = version ?? "*", // "*" = "any version"
                VersionRange = versionRange,
                TargetFramework = targetFramework,
                Type = "NuGet",
                IsTransitive = false
            };
        }
    }
}
