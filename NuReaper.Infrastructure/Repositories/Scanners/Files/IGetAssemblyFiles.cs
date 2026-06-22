namespace NuReaper.Infrastructure.Repositories.Scanners.Files
{
    public interface IGetAssemblyFiles
    {
        public List<string> Execute(string filePath);
    }
}
