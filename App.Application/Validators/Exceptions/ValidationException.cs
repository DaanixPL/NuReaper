namespace App.Application.Validators.Exceptions
{
    public class ValidationException : Exception
    {
        public string EntityName { get; }
        public object Key { get; }

        public ValidationException(string entityName, object key)
            : base($"{entityName} {key}")
        {
            EntityName = entityName;
            Key = key;
        }
    }
}