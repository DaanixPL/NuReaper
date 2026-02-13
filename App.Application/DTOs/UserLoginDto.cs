namespace App.Domain.DTOs
{
    public class UserLoginDto
    {
        public required string EmailOrUsername { get; set; }
        public required string Password { get; set; }
    }
}
