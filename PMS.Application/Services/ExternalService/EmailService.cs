
using Microsoft.Extensions.Options;
using MimeKit;
using PMS.Core.ConfigOptions;
using System.Net.Mail;

namespace PMS.Application.Services.ExternalService
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

        public async Task SendEmailWithAttachmentAsync(string recipientEmail, string subject, string body, byte[] attachmentBytes, string attachmentFileName)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailConfig.FromName, _emailConfig.FromEmail));
                message.To.Add(MailboxAddress.Parse(recipientEmail));
                message.Subject = subject;

                var builder = new BodyBuilder
                {
                    TextBody = body
                };

                //attachment excel
                if (attachmentBytes != null && attachmentBytes.Length > 0)
                {
                    builder.Attachments.Add(
                        attachmentFileName,
                        attachmentBytes,
                        ContentType.Parse("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
                }

                message.Body = builder.ToMessageBody();

                using (var client = new MailKit.Net.Smtp.SmtpClient())
                {
                    await client.ConnectAsync(
                        _emailConfig.Host,
                        int.Parse(_emailConfig.Port ?? "587"),
                        MailKit.Security.SecureSocketOptions.StartTls
                    );
                    await client.AuthenticateAsync(
                        _emailConfig.Username,
                        _emailConfig.Password
                    );
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                    Console.WriteLine($"[EmailService] Email sent to {recipientEmail}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailService] Error sending email: {ex.Message}");
                throw new Exception($"Gửi email thất bại: {ex.Message}", ex);
            }
        }

        public async Task SendMailWithPDFAsync(string subject, string body, string toEmail, byte[] attachmentBytes, string attachmentName)
        {
            using var smtpClient = new SmtpClient
            {
                Host = _emailConfig.Host!,
                Port = int.Parse(_emailConfig.Port!),
                Credentials = new System.Net.NetworkCredential(
                    _emailConfig.Username, _emailConfig.Password),
                EnableSsl = true
            };

            using var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailConfig.FromEmail, _emailConfig.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            var stream = new MemoryStream(attachmentBytes);
            var attachment = new Attachment(stream, attachmentName, "application/pdf");
            mailMessage.Attachments.Add(attachment);

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
            finally
            {
                stream.Dispose();
                attachment.Dispose();
            }
        }
    }
}
