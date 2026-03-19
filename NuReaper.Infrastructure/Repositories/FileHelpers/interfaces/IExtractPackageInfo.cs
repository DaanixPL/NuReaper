namespace NuReaper.Infrastructure.Repositories.FileHelpers.interfaces
{
    public interface IExtractPackageInfo
    {
        public (string PackageName, string Version) Execute(string url);
    }
}
