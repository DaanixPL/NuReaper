using App.Application.DTOs;

namespace NuReaper.Infrastructure.Repositories.Parsers.Utils.Interfaces
{
    public interface IParseVersionRange
    {
        public VersionRangeDto? Execute(string? versionString);
    }
}
