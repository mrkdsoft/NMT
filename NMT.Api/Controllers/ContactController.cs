using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NMT.Api.Models.Email;
using NMT.Api.Services;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace NMT.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ContactController : ControllerBase
    {
        private readonly ILogger<ContactController> _logger;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IOptions<SmtpSettings> _smtpSettings;
        private readonly string EMAIL_PATTERN = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

        public ContactController(
            ILogger<ContactController> logger,
            IEmailTemplateService emailTemplateService,
            IOptions<SmtpSettings> smtpSettings)
        {
            _logger = logger;
            _emailTemplateService = emailTemplateService;
            _smtpSettings = smtpSettings;
        }

        [Route("SendEmail")]
        [HttpPost]
        public async Task<IActionResult> SendEmail(ContactFormModel message)
        {
            // Mail mesajını oluştur
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress(_smtpSettings.Value.FromEmail); // Gönderici adresi

            _smtpSettings.Value.ToEmails.ToList().ForEach((email) =>
            {
                if (Regex.IsMatch(email, EMAIL_PATTERN))
                    mail.To.Add(email); // Alıcı adresi
            });

            mail.Subject = message.Subject; // E-posta konusu

            var emailBody = _emailTemplateService.GenerateEmailTemplate($"contact-form", new
            {
                Subject = message.Subject,
                Name = message.Name,
                Surname = message.Surname,
                Email = message.Email,
                PhoneNumber = message.PhoneNumber,
                Message = message.Message
            });

            mail.Body = emailBody; // E-posta içeriği
            mail.IsBodyHtml = true;

            // SMTP sunucusu üzerinden gönderim yapacak client'ı oluştur
            SmtpClient smtpClient = new SmtpClient(_smtpSettings.Value.Host); // SMTP sunucusunun adresi
            smtpClient.Port = _smtpSettings.Value.Port; // SMTP sunucusunun portu (genellikle 587 veya 25)
            smtpClient.EnableSsl = true; // SSL kullanılıyorsa bu değer true olmalı
            smtpClient.Credentials = new NetworkCredential(_smtpSettings.Value.Username, _smtpSettings.Value.Password); // SMTP sunucusu için kullanıcı adı ve şifre

            try
            {
                smtpClient.Send(mail); // E-postayı gönder
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { status = false, error = $"E-posta gönderimi sırasında bir hata oluştu: {ex.Message}\n{ex.StackTrace}" });
            }

            return StatusCode(200, new { status = true, error = $"E-posta başarıyla gönderildi" });
        }
    }
}
