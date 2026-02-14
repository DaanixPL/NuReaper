using System.Text.RegularExpressions;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using NuReaper.Domain.Entities;
using NuReaper.Domain.Enums;

namespace NuReaper.Infrastructure.Repositories.Scanners.Base
{
    /// <summary>
    /// Base class for all assembly scanners with common utilities
    /// </summary>
    public abstract class ScannerBase
    {
        protected static readonly Regex UrlRegex = new(
            @"https?://[^\s""'<>]+",  // Bardziej liberalny, ale bez whitespace
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected static readonly Regex OnionRegex = new(
            @"[a-z0-9\-]{3,56}\.onion",  // ✅ Akceptuj 3-56 znaków (w tym testowe domeny)
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        protected static readonly Regex Base64Regex = new(
            @"[A-Za-z0-9+/]{40,}=*", 
            RegexOptions.Compiled);

        protected static readonly Regex IpAddressRegex = new(
            @"(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)",
            RegexOptions.Compiled);

        /// <summary>
        /// Checks if a string contains suspicious patterns (URL, IP, onion, base64)
        /// </summary>
        protected virtual bool IsSuspiciousString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var lowerInput = input.ToLowerInvariant();

            return UrlRegex.IsMatch(lowerInput) ||
                   OnionRegex.IsMatch(lowerInput) ||
                   Base64Regex.IsMatch(lowerInput) ||
                   IsPrivateIP(lowerInput);
        }

        /// <summary>
        /// Checks if input contains a private IP address (IPv4 or IPv6)
        /// </summary>
        protected static bool IsPrivateIP(string input)
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

        /// <summary>
        /// Calculates danger level based on finding type and evidence
        /// </summary>
        protected virtual float CalculateDangerLevel(ScanFindingType type)
        {
            return type switch
            {
                ScanFindingType.HttpClientCall => 65f,
                ScanFindingType.WebClientCall => 70f,
                ScanFindingType.DnsCall => 55f,
                ScanFindingType.SuspiciousUrl => 60f,
                ScanFindingType.SuspiciousIpAddress => 65f,
                ScanFindingType.SuspiciousOnionAddress => 90f,
                ScanFindingType.SuspiciousBase64 => 40f,
                _ => 50f
            };
        }

        /// <summary>
        /// Calculates confidence score based on how deep the flow tracking went
        /// </summary>
        protected virtual float CalculateConfidenceScore(int hopDepth, bool isLiteralString)
        {
            // Literal strings in API calls = highest confidence
            if (isLiteralString)
                return 95f;

            // Direct use (0 hops) = very high confidence
            if (hopDepth == 0)
                return 90f;

            // Each hop reduces confidence
            return Math.Max(50f, 90f - (hopDepth * 8f));
        }
    }
}