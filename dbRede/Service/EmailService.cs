using MailKit.Net.Smtp;
using MimeKit;
using System.Threading.Tasks;

public class EmailService
{
    public async Task EnviarEmailAsync(string destinatario, string assunto, string mensagem)
    {
        var email = new MimeMessage();
        email.From.Add(MailboxAddress.Parse("devlinkcostaoliveira@gmail.com")); // Trocar pelo seu e-mail
        email.To.Add(MailboxAddress.Parse(destinatario));
        email.Subject = assunto;

        email.Body = new TextPart(MimeKit.Text.TextFormat.Plain)
        {
            Text = mensagem
        };

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
        await smtp.AuthenticateAsync("devlinkcostaoliveira@gmail.com", "aqgfrvbfwbvpqehx");
        await smtp.SendAsync(email);
        await smtp.DisconnectAsync(true);
    }
}
