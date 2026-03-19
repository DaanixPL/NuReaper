namespace NuReaper.Infrastructure.Repositories.FileHelpers.Interfaces
{
    public interface IDownloadPackageAsync
    {
        public Task<string> ExecuteAsync(string url, CancellationToken cancellationToken);
    }
}
