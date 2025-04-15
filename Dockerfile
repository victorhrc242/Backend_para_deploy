# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Cria a pasta de destino antes do COPY
RUN mkdir -p /src/dbRede

# Copia apenas o .csproj para restaurar dependências e aproveitar o cache
COPY dbRede/*.csproj dbRede/

WORKDIR /src/dbRede
RUN dotnet restore

# Copia o restante dos arquivos
COPY . .

# Publica o projeto
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "dbRede.dll"]
