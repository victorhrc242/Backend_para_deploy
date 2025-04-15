using Microsoft.Extensions.Configuration;
using Supabase;
using System;
using System.Threading.Tasks;

public class SupabaseService
{
    private readonly Client _supabase;
    // aqui esta a configuração para acessar o banco
    public SupabaseService(IConfiguration configuration)
    {
        string url = configuration["Supabase:Url"];
        string key = configuration["Supabase:Key"];

        _supabase = new Client(url, key);
    }

    public Client GetClient() => _supabase;
}
