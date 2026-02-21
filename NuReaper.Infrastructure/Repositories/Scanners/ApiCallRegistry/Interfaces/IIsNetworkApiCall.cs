namespace NuReaper.Infrastructure.Repositories.Scanners.ApiCallRegistry.Interfaces
{
    public interface IIsNetworkApiCall
    {
        bool Execute(string methodFullName);
    }
}