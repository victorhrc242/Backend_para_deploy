using Microsoft.AspNetCore.SignalR;

public class CustomUserIdProvider : IUserIdProvider
{
    public string GetUserId(HubConnectionContext connection)
    {
        // Aqui pega o userId da query string (ou você pode usar claims se estiver autenticado)
        return connection.GetHttpContext()?.Request.Query["userId"];
    }
}
