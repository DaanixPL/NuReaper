namespace NuReaper.Infrastructure.Repositories.FileHelpers.interfaces
{
    public interface IExtractPackage
    {
        public Task<string> ExecuteAsync(string tempFilePath, CancellationToken cancellationToken);
    }
}
