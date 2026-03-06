FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["EDA.API/EDA.API.csproj", "EDA.API/"]
RUN dotnet restore "EDA.API/EDA.API.csproj"

COPY . .
WORKDIR "/src/EDA.API"
RUN dotnet build "EDA.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EDA.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN addgroup --system --gid 1000 appgroup \
    && adduser --system --uid 1000 --ingroup appgroup appuser

COPY --from=publish /app/publish .
RUN chown -R appuser:appgroup /app
USER appuser

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s CMD curl --fail http://localhost:8080/health || exit 1

ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "EDA.API.dll"]
