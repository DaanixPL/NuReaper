using App.Application.DTOs;
using Microsoft.Extensions.Logging;
using NuReaper.Infrastructure.Repositories.Parsers.Utils.Interfaces;

namespace NuReaper.Infrastructure.Repositories.Parsers.Utils
{
    public class ParseVersionRange : IParseVersionRange
    {
        private readonly ILogger<ParseVersionRange> _logger;
        public ParseVersionRange(ILogger<ParseVersionRange> logger)
        {
            _logger = logger;
        }
        VersionRangeDto? IParseVersionRange.Execute(string? versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
            {
                return new VersionRangeDto
                {
                    MinVersion = null,
                    MaxVersion = null,
                    IsMinInclusive = false,
                    IsMaxInclusive = false
                };
            }

            versionString = versionString.Trim();

            if (!versionString.StartsWith("[") && !versionString.StartsWith("("))
            {
                return new VersionRangeDto
                {
                    MinVersion = versionString,
                    MaxVersion = null, 
                    IsMinInclusive = true,
                    IsMaxInclusive = false
                };
            }

            try
            {
                bool minInclusive = versionString.StartsWith("[");
                bool maxInclusive = versionString.EndsWith("]");

                var innerContent = versionString.Trim('[', ']', '(', ')');
                
                var parts = innerContent.Split(',', StringSplitOptions.TrimEntries);

                string? minVersion = parts.Length > 0 && !string.IsNullOrEmpty(parts[0]) 
                    ? parts[0] 
                    : null;

                string? maxVersion = parts.Length > 1 && !string.IsNullOrEmpty(parts[1]) 
                    ? parts[1] 
                    : null;

                return new VersionRangeDto
                {
                    MinVersion = minVersion,
                    MaxVersion = maxVersion,
                    IsMinInclusive = minInclusive,
                    IsMaxInclusive = maxInclusive
                };
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Failed to parse version range '{versionString}'", versionString);
                
                return new VersionRangeDto
                {
                    MinVersion = versionString,
                    MaxVersion = null,
                    IsMinInclusive = true,
                    IsMaxInclusive = false
                };
            }
        }
    }
}