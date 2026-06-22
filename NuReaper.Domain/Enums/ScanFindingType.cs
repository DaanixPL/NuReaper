using System.ComponentModel;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using NuReaper.Domain.Entities;

namespace NuReaper.Domain.Enums
{
    public enum ScanFindingType
    {
        MalwareDownloader,
        
        Unknown,
        None
    }
}