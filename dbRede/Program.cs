using dbRede.Hubs;
using dbRede.SignalR;
using Microsoft.AspNetCore.SignalR;
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
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
builder.Services.AddMemoryCache();

// ✅ Configure CORS corretamente
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowLocalhostWithCredentials", builder =>
    {
        builder.WithOrigins("http://localhost:5173"// url do meu localhost
            , "https://devisocial.vercel.app")// url do meu dominio hospedado
            .AllowCredentials()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
var app = builder.Build();
// ✅ Coloque o UseCors antes dos endpoints
app.UseHttpsRedirection();
app.UseCors("AllowLocalhostWithCredentials");
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "dbRede API v1");
    c.RoutePrefix = "swagger";
});
app.UseAuthorization();
// Hub do SignalR
app.MapHub<FeedHub>("/feedHub");
app.MapHub<ComentarioHub>("/comentarioHub");
app.MapHub<MensagensHub>("/mensagensHub");
app.MapHub<CurtidaHub>("/curtidaHub");
// Controllers
app.MapControllers();
app.Run();