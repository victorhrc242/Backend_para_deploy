# Etapa 1: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copia os arquivos compilados (já com o binário .dll pronto) para o diretório do contêiner
COPY ./dbRede/bin/Release/net8.0/publish/ ./


# Expõe a porta 80 para o aplicativo
EXPOSE 80

# Define o ponto de entrada para executar a aplicação
ENTRYPOINT ["dotnet", "dbRede.dll"]
