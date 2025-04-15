# Etapa 1: Runtime da imagem base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Etapa 2: Copiar os arquivos da aplicação
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["dbRede/dbRede.csproj", "dbRede/"]
RUN dotnet restore "dbRede/dbRede.csproj"
COPY . .
WORKDIR "/src/dbRede"
RUN dotnet build "dbRede.csproj" -c Release -o /app/build

# Etapa 3: Publicar os arquivos compilados
FROM build AS publish
RUN dotnet publish "dbRede.csproj" -c Release -o /app/publish

# Etapa 4: Construção final e configuração do container
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "dbRede.dll"]
