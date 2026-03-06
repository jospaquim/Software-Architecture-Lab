# Multi-stage Docker build for DDD Sales API

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY ["API/DDD.Sales.API.csproj", "API/"]
COPY ["Application/DDD.Sales.Application.csproj", "Application/"]
COPY ["Domain/DDD.Sales.Domain.csproj", "Domain/"]
COPY ["Infrastructure/DDD.Sales.Infrastructure.csproj", "Infrastructure/"]

# Restore
RUN dotnet restore "API/DDD.Sales.API.csproj"

# Copy source
COPY . .

# Build
WORKDIR "/src/API"
RUN dotnet build "DDD.Sales.API.csproj" -c Release -o /app/build

# Publish
FROM build AS publish
RUN dotnet publish "DDD.Sales.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN addgroup --system --gid 1000 appgroup \
    && adduser --system --uid 1000 --ingroup appgroup appuser

COPY --from=publish /app/publish .

RUN chown -R appuser:appgroup /app
USER appuser

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "DDD.Sales.API.dll"]
