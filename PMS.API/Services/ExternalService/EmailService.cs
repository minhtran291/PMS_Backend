
using Microsoft.Extensions.Options;
using PMS.Core.ConfigOptions;
using System.Net.Mail;

namespace PMS.API.Services.ExternalService
{
    public class EmailService : IEmailService
    {
        private readonly EmailConfig _emailConfig;
        public EmailService(IOptions<EmailConfig> options)
        {
            _emailConfig = options.Value;
            Console.WriteLine($"[Debug] EmailService config: Host={_emailConfig.Host}, Port={_emailConfig.Port}");
            Console.WriteLine($"[Debug] EmailService config: Username={_emailConfig.Username}, Password={_emailConfig.Password}");
            Console.WriteLine($"[Debug] EmailService config: FromEmail={_emailConfig.FromEmail}, FromName={_emailConfig.FromName}");
        }

        public async Task SendMailAsync(string subject, string body, string toEmail)
        {
            Console.WriteLine("Sending mail...");
            Console.WriteLine($"To: {toEmail}, Subject: {subject}");
            Console.WriteLine($"SMTP: {_emailConfig.Host}:{_emailConfig.Port}, User: {_emailConfig.Username}");

            var smtpClient = new SmtpClient
            {
                Host = _emailConfig.Host!,
                Port = int.Parse(_emailConfig.Port!),
                Credentials = new System.Net.NetworkCredential(
                    _emailConfig.Username, _emailConfig.Password),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailConfig.FromEmail, _emailConfig.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);
            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
