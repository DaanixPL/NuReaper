using System.IO.Compression;
using NuReaper.Infrastructure.Repositories.FileHelpers.interfaces;

namespace NuReaper.Infrastructure.Repositories.FileHelpers
{
    public class ExtractPackage : IExtractPackage
    {
        public string Execute(string tempFilePath)
        {
            string extractionPath = Path.Combine(
                Path.GetDirectoryName(tempFilePath) ?? Directory.GetCurrentDirectory(),
                "Extracted");

            if (Directory.Exists(extractionPath))
                Directory.Delete(extractionPath, true);

            ZipFile.ExtractToDirectory(tempFilePath, extractionPath);
            return extractionPath;
        }
    }
}
