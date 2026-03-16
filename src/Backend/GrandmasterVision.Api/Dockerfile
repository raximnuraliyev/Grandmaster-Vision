FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY GrandmasterVision.slnx .
COPY src/GrandmasterVision.Core/*.csproj src/GrandmasterVision.Core/
COPY src/Backend/GrandmasterVision.Api/*.csproj src/Backend/GrandmasterVision.Api/

# Restore
RUN dotnet restore src/Backend/GrandmasterVision.Api/GrandmasterVision.Api.csproj

# Copy source
COPY src/GrandmasterVision.Core/ src/GrandmasterVision.Core/
COPY src/Backend/GrandmasterVision.Api/ src/Backend/GrandmasterVision.Api/

# Build and publish
RUN dotnet publish src/Backend/GrandmasterVision.Api/GrandmasterVision.Api.csproj -c Release -o /app

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app

# Install Stockfish for Linux
RUN apt-get update && apt-get install -y stockfish && rm -rf /var/lib/apt/lists/*

COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
ENV STOCKFISH_PATH=/usr/games/stockfish
EXPOSE 8080

ENTRYPOINT ["dotnet", "GrandmasterVision.Api.dll"]
