# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia o arquivo .csproj para restaurar dependências
COPY dbRede/*.csproj ./dbRede/
WORKDIR /src/dbRede
RUN dotnet restore

# Copia o restante dos arquivos do projeto
COPY . .

# Publica o projeto
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copia o conteúdo publicado da etapa anterior
COPY --from=build /app/publish .

# Expõe a porta 80
EXPOSE 80

# Comando de entrada
ENTRYPOINT ["dotnet", "dbRede.dll"]
