using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.FindingCreation
{
    public class GetAssemblyFiles : IGetAssemblyFiles
    {
        public List<string> Execute(string filePath)
        {
            var files = new List<string>();

            if (Directory.Exists(filePath))
            {
                files.AddRange(Directory.GetFiles(filePath, "*.dll", SearchOption.AllDirectories));
                files.AddRange(Directory.GetFiles(filePath, "*.exe", SearchOption.AllDirectories));
            }
            else if (File.Exists(filePath))
            {
                files.Add(filePath);
            }

            return files;
        }
    }
}
