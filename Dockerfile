# Etapa 1: imagem base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Etapa 2: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar o projeto C# para a imagem Docker
COPY ./dbRede/dbRede.csproj ./dbRede/   # Certifique-se de que o caminho esteja correto
RUN dotnet restore "dbRede/dbRede.csproj"

# Copiar os arquivos restantes para o build
COPY . . 

WORKDIR "/src/dbRede"
RUN dotnet build "dbRede.csproj" -c Release -o /app/build

# Etapa 3: publicar
FROM build AS publish
RUN dotnet publish "dbRede.csproj" -c Release -o /app/publish

# Etapa 4: imagem final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "dbRede.dll"]
