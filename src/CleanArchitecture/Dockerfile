# Multi-stage Docker build for Clean Architecture API
# Optimized for production with minimal image size

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["API/CleanArchitecture.API.csproj", "API/"]
COPY ["Application/CleanArchitecture.Application.csproj", "Application/"]
COPY ["Domain/CleanArchitecture.Domain.csproj", "Domain/"]
COPY ["Infrastructure/CleanArchitecture.Infrastructure.csproj", "Infrastructure/"]

# Restore dependencies
RUN dotnet restore "API/CleanArchitecture.API.csproj"

# Copy all source code
COPY . .

# Build the application
WORKDIR "/src/API"
RUN dotnet build "CleanArchitecture.API.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "CleanArchitecture.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create non-root user for security
RUN addgroup --system --gid 1000 appgroup \
    && adduser --system --uid 1000 --ingroup appgroup appuser

# Copy published app
COPY --from=publish /app/publish .

# Change ownership
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Entry point
ENTRYPOINT ["dotnet", "CleanArchitecture.API.dll"]
