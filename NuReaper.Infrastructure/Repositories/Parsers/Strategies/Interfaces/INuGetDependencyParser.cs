using System.Xml.Linq;
using App.Application.DTOs;

namespace NuReaper.Infrastructure.Repositories.Parsers.Strategies.Interfaces
{
    public interface INuGetDependencyParser
    {
        public List<DependencyDto> ParseNuGetDependencies(XDocument xdoc, XNamespace ns);
    }
}
