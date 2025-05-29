using dbRede.Models;
using Microsoft.Extensions.Hosting;
using Supabase;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class LimpezaNotificacoesBackgroundService : BackgroundService
{
    private readonly Client _supabase;

    public LimpezaNotificacoesBackgroundService(Client supabase)
    {
        _supabase = supabase;
    }

    // Este método será chamado periodicamente para realizar a exclusão
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Vai aguardar até a próxima meia-noite antes de executar a exclusão
        var delay = GetTimeUntilMidnight();
        await Task.Delay(delay, stoppingToken);

        // Excluir notificações com mais de 30 dias
        await ExcluirNotificacoesAntigas();

        // Executar diariamente após a exclusão (aguarda 24 horas)
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            await ExcluirNotificacoesAntigas();  // Chama o método para excluir as notificações antigas
        }
    }

    // Calcula o tempo até a próxima meia-noite
    private TimeSpan GetTimeUntilMidnight()
    {
        var now = DateTime.UtcNow;
        var midnight = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, 999);
        return midnight.AddDays(1) - now;
    }

    // Método que vai excluir as notificações antigas
    private async Task ExcluirNotificacoesAntigas()
    {
        var dataLimite = DateTime.UtcNow.AddDays(-30); // Notificações com mais de 30 dias

        // Excluindo as notificações diretamente no banco de dados
        var result = await _supabase
            .From<Notificacao>()
            .Delete()  // Aplica a exclusão diretamente
            .Where($"DataEnvio <= '{dataLimite:yyyy-MM-dd HH:mm:ss}'")  // Aplica o filtro para "DataEnvio <= dataLimite"
            .Execute();  // Executa a exclusão no banco

        if (result != null && result.RowsAffected > 0)
        {
            Console.WriteLine($"{result.RowsAffected} notificações antigas excluídas.");
        }
        else
        {
            Console.WriteLine("Nenhuma notificação antiga para excluir.");
        }
    }

}
