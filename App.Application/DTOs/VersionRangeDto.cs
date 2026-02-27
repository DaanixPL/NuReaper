namespace App.Application.DTOs
{
    public class VersionRangeDto
    {
        public string? MinVersion { get; set; }
        public string? MaxVersion { get; set; }
        public bool IsMinInclusive { get; set; }
        public bool IsMaxInclusive { get; set; }

     
        public override string ToString()
        {
            if (MinVersion == null && MaxVersion == null)
                return "*";

            if (MaxVersion == null)
            {
                return IsMinInclusive 
                    ? $"≥ {MinVersion}" 
                    : $"> {MinVersion}";
            }

            if (MinVersion == null)
            {
                return IsMaxInclusive 
                    ? $"≤ {MaxVersion}" 
                    : $"< {MaxVersion}";
            }

            if (MinVersion == MaxVersion && IsMinInclusive && IsMaxInclusive)
                return $"= {MinVersion}";

            var minBracket = IsMinInclusive ? "[" : "(";
            var maxBracket = IsMaxInclusive ? "]" : ")";
            
            return $"{minBracket}{MinVersion}, {MaxVersion}{maxBracket}";
        }
    }
}
