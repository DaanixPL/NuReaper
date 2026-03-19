namespace NuReaper.Infrastructure.Repositories.GraphBuilders.Interfaces
{
    public interface IDownloadAndExtractNuspecAsync
    {
        public Task<string?> ExecuteAsync(
            string packageName, string packageVersion, CancellationToken cancellationToken);
    }
}
