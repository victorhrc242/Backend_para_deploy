# Etapa 1: build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia o .csproj para o diretório de trabalho no contêiner
COPY ./dbRede/*.csproj ./dbRede/

# Altera para o diretório do projeto
WORKDIR /src/dbRede

# Restaura as dependências do projeto
RUN dotnet restore

# Copia o restante dos arquivos do projeto para o diretório de trabalho
WORKDIR /src
COPY . .

# Publica o projeto no modo Release
WORKDIR /src/dbRede
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: runtime (a parte que executa o aplicativo)
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copia os arquivos publicados do build para o runtime
COPY --from=build /app/publish .

# Expõe a porta 80 para o aplicativo
EXPOSE 80

# Define o ponto de entrada para executar a aplicação
ENTRYPOINT ["dotnet", "dbRede.dll"]
