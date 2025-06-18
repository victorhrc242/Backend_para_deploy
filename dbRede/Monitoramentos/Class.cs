//using dbRede.Models;  // Supondo que suas mensagens estejam nesse namespace
//using Microsoft.Extensions.Hosting;
//using Supabase;
//using System;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;

//public class LimpezaMensagensBackgroundService : BackgroundService
//{
//    private readonly Client _supabase;

//    public LimpezaMensagensBackgroundService(Client supabase)
//    {
//        _supabase = supabase;
//    }

//    // Este método será chamado periodicamente para realizar a exclusão
//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        // Rodar a tarefa a cada 10 horas
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            // Executar a exclusão a cada 10 horas
//            await ExcluirMensagensAntigas();

//            // Aguarda 10 horas antes de rodar novamente
//            await Task.Delay(TimeSpan.FromHours(10), stoppingToken);
//        }
//    }

//    // Método que vai excluir as mensagens com mais de 10 horas
//    private async Task ExcluirMensagensAntigas()
//    {
//        var dataLimite = DateTime.UtcNow.AddHours(-10);  // Mensagens com mais de 10 horas

//        // Excluindo as mensagens marcadas como apagadas e com mais de 10 horas
//        var result = await _supabase
//            .From<Mensagens>()
//            .Where(n => n.data_envio <= dataLimite && n.apagada == true)  // Filtro para mensagens apagadas com mais de 10 horas
//            .Delete();  // Aplica a exclusão das mensagens

//        // Verificando o resultado da operação de exclusão
//        if (result.RowsAffected > 0)  // Verifica quantas linhas foram afetadas
//        {
//            Console.WriteLine($"{result.RowsAffected} mensagens apagadas e antigas excluídas.");
//        }
//        else
//        {
//            Console.WriteLine("Nenhuma mensagem apagada e antiga para excluir.");
//        }
//    }


//}

