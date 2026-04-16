# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY RAG.slnx ./RAG.slnx
COPY Directory.Build.props ./Directory.Build.props

COPY RAG.Domain/RAG.Domain.csproj ./RAG.Domain/RAG.Domain.csproj
COPY RAG.Application/RAG.Application.csproj ./RAG.Application/RAG.Application.csproj
COPY RAG.Infrastructure/RAG.Infrastructure.csproj ./RAG.Infrastructure/RAG.Infrastructure.csproj
COPY RAG.Api/RAG.Api.csproj ./RAG.Api/RAG.Api.csproj
COPY RAG.Tests/RAG.Tests.csproj ./RAG.Tests/RAG.Tests.csproj

RUN dotnet restore

COPY RAG.Domain ./RAG.Domain
COPY RAG.Application ./RAG.Application
COPY RAG.Infrastructure ./RAG.Infrastructure
COPY RAG.Api ./RAG.Api
COPY RAG.Tests ./RAG.Tests

RUN dotnet build -c Release -o /app/build


# Publish stage
FROM build AS publish

RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false


# UI build stage
FROM node:20-alpine AS ui-builder

WORKDIR /app/ui

COPY RAG.UI/package*.json ./

RUN npm install && npm cache clean --force

COPY RAG.UI ./

RUN npm run build


# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd -r appgroup && useradd -r -g appgroup appuser

COPY --from=publish /app/publish ./app
COPY --from=ui-builder /app/ui/dist/rag-ui/browser ./app/wwwroot

RUN chown -R appuser:appgroup /app

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

CMD ["dotnet", "app/RAG.Api.dll"]