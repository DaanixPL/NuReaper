namespace NuReaper.Domain.Entities
{
    public class ScanJobStatus
    {
        public required string Status { get; set; }
        public ScanPackageResult? Result { get; set; }
        public string? ErrorMessage { get; set; }
    }
}