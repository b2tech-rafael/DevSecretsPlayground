# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj first for restore caching
COPY ./DevSecretsPlayground.csproj DevSecretsPlayground/
RUN dotnet restore DevSecretsPlayground/DevSecretsPlayground.csproj

# Copy project and publish
COPY . DevSecretsPlayground/
WORKDIR /src/DevSecretsPlayground
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DevSecretsPlayground.dll"]