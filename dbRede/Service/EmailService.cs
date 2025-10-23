using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

public class EmailService
{
    public async Task EnviarEmailAsync(string destinatario, string assunto, string mensagem)
    {
        var remetente = "devlinkcostaoliveira@gmail.com";
        var senhaApp = "aqgfrvbfwbvpqehx"; // ⚠️ não é a senha normal!

        using var smtp = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new NetworkCredential(remetente, senhaApp),
            EnableSsl = true
        };

        var mail = new MailMessage(remetente, destinatario, assunto, mensagem)
        {
            IsBodyHtml = true
        };

        await smtp.SendMailAsync(mail);
    }
}
