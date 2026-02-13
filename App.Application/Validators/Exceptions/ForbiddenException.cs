namespace App.Application.Validators.Exceptions
{
    public class ForbiddenException : Exception
    {
        public string EntityName { get; set; }
        public object Key { get; set; }

        public ForbiddenException(string entityName, object key) : base($"{entityName} {key} is forbidden") 
        {
            EntityName = entityName;
            Key = key;
        }
    }
}
