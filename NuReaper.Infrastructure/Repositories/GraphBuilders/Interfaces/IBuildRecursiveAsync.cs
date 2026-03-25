using System.Collections.Immutable;
using NuReaper.Application.DTOs;
using NuReaper.Infrastructure.Repositories.GraphBuilders.HelperClasses;

namespace NuReaper.Infrastructure.Repositories.GraphBuilders.Interfaces
{
    public interface IBuildRecursiveAsync
    {
        public Task Execute(
            string packageName,
            string packageVersion,
            string nuspecPath,
            string? targetFramework,
            GraphBuildingContext context,
            ImmutableStack<string> currentPath,
            int depth,
            int maxDepth,
            CancellationToken cancellationToken);
    }
}
