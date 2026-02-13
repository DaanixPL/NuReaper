namespace App.Application.Validators.Exceptions
{
    public class NotFoundException : Exception
    {
        public string EntityName { get; }
        public object Key { get; }

        public NotFoundException(string entityName, object key)
            : base($"{entityName} {key} was not found.")
        {
            EntityName = entityName;
            Key = key;
        }
    }
}
