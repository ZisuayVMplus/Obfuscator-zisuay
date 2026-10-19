# ─── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and projects
COPY ["77fuscator NEW src/geniussolution.sln", "77fuscator NEW src/"]
COPY ["77fuscator NEW src/77main/geniussolution.csproj", "77fuscator NEW src/77main/"]
COPY ["src/77fuscatorWeb/77fuscatorWeb.csproj", "src/77fuscatorWeb/"]

# Restore dependencies
RUN dotnet restore "src/77fuscatorWeb/77fuscatorWeb.csproj"

# Copy source
COPY . .

# Build & publish
RUN dotnet publish "src/77fuscatorWeb/77fuscatorWeb.csproj" \
    -c Release -o /app/publish --no-restore

# Install darklua (required by the obfuscator pipeline)
RUN apt-get update && apt-get install -y curl && \
    curl -sL "https://github.com/seaofvoices/darklua/releases/latest/download/darklua-linux-x86_64.tar.gz" \
    | tar -xz -C /usr/local/bin darklua && \
    chmod +x /usr/local/bin/darklua

# ─── Runtime stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy darklua from build stage
COPY --from=build /usr/local/bin/darklua /usr/local/bin/darklua

# Copy published app
COPY --from=build /app/publish .

# Copy darkluaconfig.json (needed by obfuscator)
COPY ["77fuscator NEW src/77main/darkluaconfig.json", "./darkluaconfig.json"]

# Render uses PORT env var
ENV ASPNETCORE_URLS=http://+:${PORT:-10000}
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 10000

ENTRYPOINT ["dotnet", "77fuscatorWeb.dll"]
