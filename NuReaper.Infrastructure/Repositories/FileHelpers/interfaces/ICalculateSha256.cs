namespace NuReaper.Infrastructure.Repositories.FileHelpers.interfaces
{
    public interface ICalculateSha256
    {
        public string Execute(string filePath);
    }
}
