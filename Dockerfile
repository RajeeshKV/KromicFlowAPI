# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Build image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first (for Docker layer caching)
COPY ["src/KromicFlow.Domain/KromicFlow.Domain.csproj", "src/KromicFlow.Domain/"]
COPY ["src/KromicFlow.Application/KromicFlow.Application.csproj", "src/KromicFlow.Application/"]
COPY ["src/KromicFlow.Infrastructure/KromicFlow.Infrastructure.csproj", "src/KromicFlow.Infrastructure/"]
COPY ["src/KromicFlow.Api/KromicFlow.Api.csproj", "src/KromicFlow.Api/"]

# Restore only the API project (its project references will be restored automatically)
RUN dotnet restore "src/KromicFlow.Api/KromicFlow.Api.csproj"

# Copy the remaining source code
COPY . .

# Publish the API
RUN dotnet publish "src/KromicFlow.Api/KromicFlow.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# Final image
FROM base AS final
WORKDIR /app

COPY --from=build /app/publish .
COPY ["EFMigration.sh", "./EFMigration.sh"]

RUN chmod +x ./EFMigration.sh

ENTRYPOINT ["dotnet", "KromicFlow.Api.dll"]