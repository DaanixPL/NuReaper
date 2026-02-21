using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.Scanners.FindingCreation.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.FindingCreation
{
    public class GetFindingType : IGetFindingType
    {
        public ScanFindingType Execute(string apiCall)
        {
            if (apiCall.Contains("HttpClient"))
                return ScanFindingType.HttpClientCall;
            if (apiCall.Contains("WebClient"))
                return ScanFindingType.WebClientCall;
            if (apiCall.Contains("Dns::GetHost"))
                return ScanFindingType.DnsCall;
            if (apiCall.Contains("TcpClient"))
                return ScanFindingType.TcpClientCall;
            if (apiCall.Contains("WebSocket"))
                return ScanFindingType.WebSocketCall;

            return ScanFindingType.Unknown;
        }
    }
}
