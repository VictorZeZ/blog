using blog.Domain.Common.Interfaces;
using blog.Domain.Common.Settings;
using blog.Domain.Exceptions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace blog.Infrastructure.Services
{
    public class SmtpEmailSender(IOptions<EmailSettings> settings) : IEmailSender
    {
        private readonly EmailSettings _settings = settings.Value;

        public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();

            try
            {
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls, ct);
                await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword, ct);
                await client.SendAsync(message, ct);
            }
            catch (Exception)
            {
                throw new UnavailableException("Email");
            }
            //catch (Exception ex)
            //{
            //    throw new ArgumentException(nameof(ex));
            //}
            finally
            {
                await client.DisconnectAsync(true, ct);
            }
        }
    }
}
