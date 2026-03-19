using System.Security.Cryptography;
using NuReaper.Infrastructure.Repositories.FileHelpers.interfaces;

namespace NuReaper.Infrastructure.Repositories.FileHelpers
{
    public class CalculateSha256 : ICalculateSha256
    {
        public string Execute(string filePath)
        {
            using (var sha256 = SHA256.Create())
            using (var fileStream = File.OpenRead(filePath))
            {
                byte[] hashBytes = sha256.ComputeHash(fileStream);
                return Convert.ToHexString(hashBytes).ToLowerInvariant();
            }
        }
    }
}
