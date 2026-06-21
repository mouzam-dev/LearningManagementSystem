# syntax=docker/dockerfile:1.6
# Production image for Fly.io — builds the Angular SPA and the .NET 8 API, then
# serves BOTH from a single container (same origin; HTTPS handled by Fly's edge).
# Build context is the repo root.

# ---- Stage 1: Angular production build (apiUrl '/api' via environment.prod.ts) ----
FROM node:20-alpine AS web
WORKDIR /web
COPY lms-angular/package.json lms-angular/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY lms-angular/ ./
RUN npx ng build --configuration production

# ---- Stage 2: restore + publish the API ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/LMS.Domain/LMS.Domain.csproj",                 "src/LMS.Domain/"]
COPY ["src/LMS.Application/LMS.Application.csproj",       "src/LMS.Application/"]
COPY ["src/LMS.Infrastructure/LMS.Infrastructure.csproj", "src/LMS.Infrastructure/"]
COPY ["src/LMS.WebAPI/LMS.WebAPI.csproj",                 "src/LMS.WebAPI/"]
RUN dotnet restore "src/LMS.WebAPI/LMS.WebAPI.csproj"
COPY src/ src/
RUN dotnet publish "src/LMS.WebAPI/LMS.WebAPI.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- Stage 3: runtime — SPA + API in one container ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# QuestPDF/SkiaSharp need native font libraries on Linux for certificate PDFs.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 fontconfig fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

# Run as the non-root user the base image ships with.
USER $APP_UID

COPY --from=build /app/publish ./
# Drop the Angular production build into wwwroot so one origin serves the SPA + API.
# (Program.cs MapFallback returns index.html for client-side routes in Production.)
COPY --from=web /web/dist/lms-angular/browser ./wwwroot

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080
ENTRYPOINT ["dotnet", "LMS.WebAPI.dll"]
