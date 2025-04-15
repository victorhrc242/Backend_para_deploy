# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia apenas o .csproj para o restore funcionar e aproveitar cache
COPY ./dbRede/*.csproj ./dbRede/
WORKDIR /src/dbRede
RUN dotnet restore

# Copia o restante dos arquivos do projeto
WORKDIR /src
COPY . .

# Publica o projeto
WORKDIR /src/dbRede
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "dbRede.dll"]
