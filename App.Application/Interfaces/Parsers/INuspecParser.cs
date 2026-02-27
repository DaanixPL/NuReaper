using App.Application.DTOs;

namespace NuReaper.Application.Interfaces.Parsers
{
    public interface INuspecParser
    {
        Task<List<DependencyDto>> ParseDependenciesAsync(string nuspecFilePath, CancellationToken cancellationToken);
    }
}
