using dbRede.Hubs;
using dbRede.SignalR;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Serviços
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "dbRede API",
        Version = "v1"
    });
});

// Configura Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetSection("Redis")["ConnectionString"];
    return ConnectionMultiplexer.Connect(configuration);
});

builder.Services.AddSingleton<SupabaseService>();
builder.Services.AddSignalR();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
builder.Services.AddMemoryCache();

// ✅ Configuração CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhostWithCredentials", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",           // Localhost para dev
                "https://olicorpparadise.vercel.app" // Produção (Vercel)
            )
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ✅ Ordem correta dos middlewares
app.UseCors("AllowLocalhostWithCredentials"); // <-- deve vir logo aqui!

app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "dbRede API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthorization();

// Hubs do SignalR
app.MapHub<FeedHub>("/feedHub");
app.MapHub<ComentarioHub>("/comentarioHub");
app.MapHub<MensagensHub>("/mensagensHub");
app.MapHub<CurtidaHub>("/curtidaHub");

// Controllers
app.MapControllers();

app.Run();
