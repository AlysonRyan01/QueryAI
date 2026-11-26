# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG VERSION=0.1.0
WORKDIR /source

COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/QueryAI.Domain/QueryAI.Domain.csproj src/QueryAI.Domain/
COPY src/QueryAI.Application/QueryAI.Application.csproj src/QueryAI.Application/
COPY src/QueryAI.Infrastructure/QueryAI.Infrastructure.csproj src/QueryAI.Infrastructure/
COPY src/QueryAI.Api/QueryAI.Api.csproj src/QueryAI.Api/
RUN dotnet restore src/QueryAI.Api/QueryAI.Api.csproj

COPY src/ src/
RUN dotnet publish src/QueryAI.Api/QueryAI.Api.csproj \
      -c Release --no-restore -o /app \
      -p:Version=${VERSION} -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
ARG VERSION=0.1.0
LABEL org.opencontainers.image.title="QueryAI API" \
      org.opencontainers.image.version="${VERSION}"

RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

EXPOSE 8080
USER app

HEALTHCHECK --interval=15s --timeout=5s --start-period=60s --retries=5 \
  CMD curl -fsS http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["dotnet", "QueryAI.Api.dll"]
