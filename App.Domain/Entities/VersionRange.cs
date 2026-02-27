namespace App.Domain.Entities
{
    public class VersionRange
    {
        public Guid Id { get; set; }

        public string? MinVersion { get; set; }
        public string? MaxVersion { get; set; }
        public bool IsMinInclusive { get; set; }
        public bool IsMaxInclusive { get; set; }
    }
}
