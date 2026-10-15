# syntax=docker/dockerfile:1
# Contexto de build: raiz do repositorio (ver deploy/docker-compose.yml)

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore em camada separada: so invalida quando um .csproj muda
COPY src/CacheFinancas.slnx src/
COPY src/CacheFinancas/CacheFinancas.csproj src/CacheFinancas/
COPY src/CacheFinancas.Application/CacheFinancas.Application.csproj src/CacheFinancas.Application/
COPY src/CacheFinancas.Domain/CacheFinancas.Domain.csproj src/CacheFinancas.Domain/
COPY src/CacheFinancas.Infrastructure/CacheFinancas.Infrastructure.csproj src/CacheFinancas.Infrastructure/
RUN dotnet restore src/CacheFinancas/CacheFinancas.csproj

COPY src/ src/
RUN dotnet publish src/CacheFinancas/CacheFinancas.csproj \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0 \
    DataProtection__KeysPath=/keys

# Diretorio de chaves pertence ao usuario nao-root (volume nomeado herda o dono)
RUN mkdir /keys && chown $APP_UID /keys

COPY --from=build --chown=$APP_UID /app/publish .
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "CacheFinancas.dll"]
