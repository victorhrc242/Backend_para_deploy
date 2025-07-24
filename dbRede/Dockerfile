# Etapa 1: imagem base com ASP.NET
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# Instala o Python e dependências
RUN apt-get update && \
    apt-get install -y python3 python3-pip && \
    pip3 install --break-system-packages --no-cache-dir numpy scikit-learn joblib


# Cria alias: python → python3
RUN ln -s /usr/bin/python3 /usr/bin/python

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

# Etapa 4: imagem final com tudo
FROM base AS final
WORKDIR /app

# Copia aplicação publicada e scripts Python/modelo
COPY --from=publish /app/publish .
COPY prever_feed.py .
COPY modelo_feed.joblib .

ENTRYPOINT ["dotnet", "dbRede.dll"]
