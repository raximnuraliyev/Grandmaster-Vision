FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY GrandmasterVision.slnx .
COPY src/GrandmasterVision.Core/*.csproj src/GrandmasterVision.Core/
COPY src/Frontend/GrandmasterVision.Client/*.csproj src/Frontend/GrandmasterVision.Client/

# Restore
RUN dotnet restore src/Frontend/GrandmasterVision.Client/GrandmasterVision.Client.csproj

# Copy source
COPY src/GrandmasterVision.Core/ src/GrandmasterVision.Core/
COPY src/Frontend/GrandmasterVision.Client/ src/Frontend/GrandmasterVision.Client/

# Build and publish Blazor WASM
RUN dotnet publish src/Frontend/GrandmasterVision.Client/GrandmasterVision.Client.csproj \
    -c Release \
    -o /app/publish

# Serve with nginx
FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY src/Frontend/GrandmasterVision.Client/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
