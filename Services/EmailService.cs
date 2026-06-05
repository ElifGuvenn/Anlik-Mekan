using MailKit.Net.Smtp;
using MimeKit;

namespace AnlikMekanCore.Services;

public class EmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task GonderAsync(string alici, string konu, string mesaj)
    {
        var host = _config["Email:Host"];
        if (string.IsNullOrEmpty(host)) return; // E-posta yapılandırılmamış, sessizce geç

        try
        {
            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(_config["Email:From"] ?? "noreply@anlikmekan.com"));
            email.To.Add(MailboxAddress.Parse(alici));
            email.Subject = konu;
            email.Body = new TextPart("plain") { Text = mesaj };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, int.Parse(_config["Email:Port"] ?? "587"),
                MailKit.Security.SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config["Email:User"], _config["Email:Password"]);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
        catch
        {
            // Üretimde loglama eklenebilir
        }
    }
}
