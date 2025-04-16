# Etapa 1: Construir o aplicativo
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Definir o diretório de trabalho dentro do contêiner
WORKDIR /app

# Copiar o arquivo de projeto .csproj e restaurar as dependências
COPY *.csproj ./
RUN dotnet restore

# Copiar todos os arquivos restantes do projeto, incluindo dbRede
COPY . ./

# Publicar o aplicativo em modo Release
RUN dotnet publish -c Release -o /app/publish

# Etapa 2: Criar a imagem para rodar o aplicativo
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Definir o diretório de trabalho dentro do contêiner para a aplicação rodar
WORKDIR /app

# Copiar os arquivos do build para a imagem final
COPY --from=build /app/publish .

# Copiar o diretório dbRede para dentro do contêiner
# Isso só vai funcionar se o diretório dbRede estiver no mesmo nível do Dockerfile
COPY dbRede /app/dbRede

# Expor a porta que o aplicativo vai escutar
EXPOSE 80

# Comando para rodar o aplicativo
ENTRYPOINT ["dotnet", "SeuProjeto.dll"]
