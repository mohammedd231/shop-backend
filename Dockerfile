# Use official .NET 8 runtime as base
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Use SDK for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy everything
COPY . .

# Build the API project
RUN dotnet restore ShopApplication/ShopApplication.Api/ShopApplication.Api.csproj
RUN dotnet publish ShopApplication/ShopApplication.Api/ShopApplication.Api.csproj -c Release -o /app/publish

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ShopApplication.Api.dll"]
