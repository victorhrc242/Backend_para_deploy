# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia apenas o .csproj para restaurar dependências e aproveitar o cache
COPY Backend/dbRede/*.csproj Backend/dbRede/

WORKDIR /src/Backend/dbRede
RUN dotnet restore

# Copia o restante dos arquivos
COPY backend/ .

# Publica o projeto
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "dbRede.dll"]
