FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["KromicFlowAPI.sln", "."]
COPY ["src/KromicFlow.Domain/KromicFlow.Domain.csproj", "src/KromicFlow.Domain/"]
COPY ["src/KromicFlow.Application/KromicFlow.Application.csproj", "src/KromicFlow.Application/"]
COPY ["src/KromicFlow.Infrastructure/KromicFlow.Infrastructure.csproj", "src/KromicFlow.Infrastructure/"]
COPY ["src/KromicFlow.Api/KromicFlow.Api.csproj", "src/KromicFlow.Api/"]
RUN dotnet restore "KromicFlowAPI.sln"
COPY . .
RUN dotnet publish "src/KromicFlow.Api/KromicFlow.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY ["EFMigration.sh", "./EFMigration.sh"]
ENTRYPOINT ["dotnet", "KromicFlow.Api.dll"]
