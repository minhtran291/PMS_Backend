namespace PMS.Application.Services.ExternalService
{
    public interface IEmailService
    {
        Task SendMailAsync(string subject, string body, string toEmail);
    }
}
