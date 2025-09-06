# Etapa 1: imagem base com ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Atualiza certificados raiz (importante para TLS MongoDB Atlas)
RUN apt-get update && \
    apt-get install -y ca-certificates && \
    update-ca-certificates

# Etapa 2: build da aplicação
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY dbRede.csproj ./
RUN dotnet restore "dbRede.csproj"
COPY . .
RUN dotnet build "dbRede.csproj" -c Release -o /app/build

# Etapa 3: publish
FROM build AS publish
RUN dotnet publish "dbRede.csproj" -c Release -o /app/publish

# Etapa 4: imagem final
FROM base AS final
WORKDIR /app

# Copia a aplicação publicada
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "dbRede.dll"]
