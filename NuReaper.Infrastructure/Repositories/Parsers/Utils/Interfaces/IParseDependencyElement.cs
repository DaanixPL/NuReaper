using System.Xml.Linq;
using App.Application.DTOs;

namespace NuReaper.Infrastructure.Repositories.Parsers.Utils.Interfaces
{
    public interface IParseDependencyElement
    {
        public DependencyDto? Execute(XElement element, string? targetFramework);
    }
}
