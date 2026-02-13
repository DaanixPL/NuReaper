namespace App.Application.Validators.ValidationMessages
{
    public static class ValidationMessage
    {
        public static string Required(string field) => $"{field} is required.";
        public static string TooLong(string field, int max) => $"{field} must be at most {max} characters.";
        public static string TooShort(string field, int min) => $"{field} must be at least {min} characters.";
        public static string InvalidEmail(string field) => $"{field} is not a valid email address.";
        public static string WhiteSpaces(string field) => $"{field} cannot contain spaces.";
        public static string isUnique(string field) => $"{field} is already exists.";
    }
}
