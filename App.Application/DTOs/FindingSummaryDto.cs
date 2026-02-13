namespace NuReaper.Application.DTOs
{
    public record FindingSummaryDto
    {
        public required string Type { get; set; }

        public float DangerousLevel { get; set; }

        public string? Evidence { get; set; }
        public string? RawData { get; set; }
        public string? Location { get; set; }
    }
}