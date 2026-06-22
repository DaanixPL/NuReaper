namespace NuReaper.Application.Responses
{
    public record ScanJobStatusResponse
    {
        public required string Status { get; set; }
        public ScanPackageResultResponse? Result { get; set; }
        public string? ErrorMessage { get; set; }
        public string? DataFlowGraphId { get; set; }
    }
}