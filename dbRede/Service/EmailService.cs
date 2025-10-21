using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class EmailService
{
    private readonly string _apiKey = "re_9wjVbG7A_8EZiHGhej678HqVy24nfHnny"; // ⚠️ Substitua pela sua chave

    public async Task EnviarEmailAsync(string destinatario, string assunto, string mensagem)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var body = new
        {
            from = "devlinkcostaoliveira@gmail.com", // use um domínio ou Gmail aqui
            to = new[] { destinatario },
            subject = assunto,
            html = $"<p>{mensagem}</p>"
        };

        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://api.resend.com/emails", content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new System.Exception($"Falha ao enviar e-mail: {response.StatusCode} - {error}");
        }
    }
}
