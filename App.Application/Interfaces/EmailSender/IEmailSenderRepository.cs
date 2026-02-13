namespace App.Application.Interfaces.EmailSender
{
    public interface IEmailSenderRepository
    {
        Task SendEmailAsync(string email, string subject, string body, string? token, CancellationToken cancellationToken = default);
        Task SendEmailsAsync(IEnumerable<string> tos, string subject, string message, CancellationToken cancellationToken = default);
    }
}