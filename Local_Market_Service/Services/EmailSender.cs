using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace Local_Market_Service.Services
{
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration config;

        public EmailSender(IConfiguration config)
        {
            this.config = config;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpServer = config["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(config["EmailSettings:SmtpPort"] ?? "587");
            var smtpUser = config["EmailSettings:SmtpUser"];
            var smtpPass = config["EmailSettings:SmtpPass"];

           
            var client = new SmtpClient(smtpServer, smtpPort)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUser, smtpPass)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUser),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);

            return client.SendMailAsync(mailMessage);
        }
    }
}
