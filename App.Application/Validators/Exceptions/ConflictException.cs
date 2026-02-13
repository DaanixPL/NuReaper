namespace App.Application.Validators.Exceptions
{
    public class ConflictException : Exception
    {
        public string EntityName { get; set; }
        public object Key { get; set; }

        public ConflictException(string entityName, object key) : base($"{entityName} {key} is already in use")
        {
            EntityName = entityName;
            Key = key;
        }
    }
}
