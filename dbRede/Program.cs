using dbRede.Hubs;

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

builder.Services.AddSingleton<SupabaseService>();
builder.Services.AddSignalR();

// ✅ Configure CORS corretamente
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder
            .WithOrigins("http://localhost:5173") // frontend local
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials(); // se usar cookies/autenticação
    });
});

var app = builder.Build();

// ✅ Coloque o UseCors antes dos endpoints
app.UseCors("AllowAll");

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "dbRede API v1");
    c.RoutePrefix = "swagger";
});

app.UseHttpsRedirection();
app.UseAuthorization();

// Hub do SignalR
app.MapHub<FeedHub>("/feedHub");

// Controllers
app.MapControllers();

app.Run();
