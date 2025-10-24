namespace PMS.Application.Services.ExternalService
{
    public interface IEmailService
    {
        Task SendMailAsync(string subject, string body, string toEmail);
        Task SendEmailWithAttachmentAsync(string recipientEmail, string subject, string body, byte[] attachmentBytes, string attachmentFileName);
        Task SendMailWithPDFAsync(string subject, string body, string toEmail, byte[] attachmentBytes, string attachmentName);
    }
}
