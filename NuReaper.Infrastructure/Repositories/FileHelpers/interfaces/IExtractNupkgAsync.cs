namespace NuReaper.Infrastructure.Repositories.FileHelpers.interfaces
{
    public interface IExtractNupkgAsync
    {
        Task<string> ExecuteAsync(string nupkgPath, CancellationToken cancellationToken);
    }
}
