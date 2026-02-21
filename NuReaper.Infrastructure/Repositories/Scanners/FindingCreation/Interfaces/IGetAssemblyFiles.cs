namespace NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces
{
    public interface IGetAssemblyFiles
    {
        public List<string> Execute(string filePath);
    }
}
