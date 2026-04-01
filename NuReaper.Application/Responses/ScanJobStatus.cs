namespace NuReaper.Application.Responses
{
    public record ScanJobStatus
    {
        public required string Status { get; set; }
        public ScanPackageResultResponse? Result { get; set; }
        public string? ErrorMessage { get; set; }
    }
}