using System.Text.RegularExpressions;
using NuReaper.Domain.Enums;
using NuReaper.Infrastructure.Repositories.Scanners.Patterns.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Scanners.Patterns
{
    public class PatternRegistry : IPatternRegistry
    {
          private readonly Regex UrlRegex = new(
            @"(?:https?|wss?|ftp|ftps|ssh|telnet)://[^\s""'<>]+",  
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex OnionRegex = new(
            @"[a-z0-9\-]{3,56}\.onion", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly Regex Base64Regex = new(
            @"[A-Za-z0-9+/]{40,}=*", 
            RegexOptions.Compiled);

        private readonly Regex IpAddressRegex = new(
            @"(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)",
            RegexOptions.Compiled);

        private readonly Regex HostnameRegex = new(
            @"(?:[a-zA-Z0-9](?:[a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}",
            RegexOptions.Compiled);

        private readonly string[] SuspiciousHostnameTLDs = new[]
        {
            ".onion", ".tk", ".ml", ".ga", ".cf", ".gq",  // Free/dark TLDs
            ".top", ".xyz", ".club", ".live", ".work", ".online",     // Cheap TLDs popular with malware
            "pastebin.com", "hastebin.com", "discord.gg",  // File sharing/C2 infrastructure
            "ngrok.io", "serveo.net", "localtunnel.me"     // Tunneling services
        };
        private static readonly string[] HighRiskBareApiCalls = new[]
        {
            "TcpClient::Connect",
            "TcpClient::ConnectAsync",
            "Socket::Connect",
            "Socket::ConnectAsync",
            "UdpClient::Connect",
            "ClientWebSocket::ConnectAsync",
            "WebSocket::ConnectAsync",
            "TcpListener::Start",
            "Socket::Bind",
            "NamedPipeClientStream::Connect",
            "Process::Start",  // Always suspicious
            "ServicePointManager::set_ServerCertificateValidationCallback",  // Cert bypass
        };


        public ScanFindingType IsSuspiciousString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return ScanFindingType.None;

            var lowerInput = input.ToLowerInvariant();

            if (UrlRegex.IsMatch(lowerInput))
                return ScanFindingType.SuspiciousUrl;
            if (OnionRegex.IsMatch(lowerInput))
                return ScanFindingType.SuspiciousOnionAddress;
            if (Base64Regex.IsMatch(lowerInput))
                return ScanFindingType.SuspiciousBase64;
            if (IsPrivateIP(lowerInput))
                return ScanFindingType.SuspiciousIpAddress;
            if (IsSuspiciousHostname(lowerInput))
                return ScanFindingType.SuspiciousUrl;

            return ScanFindingType.None;
        }
        public bool IsSuspiciousHostname(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var lowerInput = input.ToLowerInvariant();

            // Check if it's a bare hostname (no scheme)
            if (!UrlRegex.IsMatch(lowerInput) && HostnameRegex.IsMatch(lowerInput))
            {
                // Check for suspicious TLDs
                foreach (var tld in SuspiciousHostnameTLDs)
                {
                    if (lowerInput.Contains(tld))
                        return true;
                }
                
                // Hostname is present but not in suspicious list = medium risk
                // Check if it's followed by port number (common for C2)
                return true; 
            }

            return false;
        }
        public bool IsPrivateIP(string input)
        {
            var matches = IpAddressRegex.Matches(input);

            foreach (Match match in matches)
            {
                if (match.Value is { } value && !string.IsNullOrWhiteSpace(value))
                {
                    if (System.Net.IPAddress.TryParse(value, out var ip))
                    {
                        byte[] bytes = ip.GetAddressBytes();

                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) // IPv4
                        {
                            if (bytes[0] == 10 ||
                                (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                                (bytes[0] == 192 && bytes[1] == 168))
                            {
                                return true;
                            }
                        }
                        else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) // IPv6
                        {
                            // ULA (Unique Local Address) fc00::/7
                            if ((bytes[0] == 0xfc || bytes[0] == 0xfd) && (bytes[1] & 0xfe) == 0xc0)
                                return true;

                            // Link-local fe80::/10
                            if (bytes[0] == 0xfe && (bytes[1] & 0xc0) == 0x80)
                                return true;
                        }
                    }
                }
            }
            return false;
        }
        public bool IsHighRiskBareApiCall(string methodFullName)
        {
            return HighRiskBareApiCalls.Any(api => methodFullName.Contains(api));
        }
    }
}
