# syntax=docker/dockerfile:1.6
# Build context for this Dockerfile is the repo root, so paths are repo-relative.

# ---------------------------------------------------------------------------
# Stage 1 — restore + publish using the .NET 8 SDK
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files first so `dotnet restore` is cached unless dependencies change.
COPY ["src/LMS.Domain/LMS.Domain.csproj",                 "src/LMS.Domain/"]
COPY ["src/LMS.Application/LMS.Application.csproj",       "src/LMS.Application/"]
COPY ["src/LMS.Infrastructure/LMS.Infrastructure.csproj", "src/LMS.Infrastructure/"]
COPY ["src/LMS.WebAPI/LMS.WebAPI.csproj",                 "src/LMS.WebAPI/"]
RUN dotnet restore "src/LMS.WebAPI/LMS.WebAPI.csproj"

# Now copy the source and publish.
COPY src/ src/
RUN dotnet publish "src/LMS.WebAPI/LMS.WebAPI.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------------------------------------------------------------------------
# Stage 2 — slim ASP.NET Core 8 runtime
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# QuestPDF/SkiaSharp (certificate PDFs) need native font libraries on Linux; the
# slim runtime image ships none, so PDF generation would otherwise fail. Install
# fontconfig + a base font set. Runs as root before dropping to the app user.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 fontconfig fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*

# Run as the non-root user that the base image ships with.
USER $APP_UID

COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Development \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

ENTRYPOINT ["dotnet", "LMS.WebAPI.dll"]
