# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia tudo e entra na pasta do projeto
COPY . .
WORKDIR /src/dbRede

# Restaura, compila e publica
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: runtime (ATENÇÃO AQUI!)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "dbRede.dll"]
