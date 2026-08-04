using LogInLab.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;

namespace LogInLab.Infrastructure.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SmtpEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            string? host = _configuration["Smtp:Host"];
            int.TryParse(_configuration["Smtp:Port"], out int port);
            string? fromEmail = _configuration["Smtp:FromEmail"];
            string? fromName = _configuration["Smtp:FromName"];

            using SmtpClient client = new SmtpClient(host, port);
            using MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName), 
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);    
            await client.SendMailAsync(mailMessage);
        }
    }
}
