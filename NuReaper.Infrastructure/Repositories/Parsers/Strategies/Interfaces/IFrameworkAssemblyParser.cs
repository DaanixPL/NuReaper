using System.Xml.Linq;
using App.Application.DTOs;

namespace NuReaper.Infrastructure.Repositories.Parsers.Strategies.Interfaces
{
    public interface IFrameworkAssemblyParser
    {
        public List<DependencyDto> ParseFrameworkAssemblies(XDocument xdoc, XNamespace ns);
    }
}
