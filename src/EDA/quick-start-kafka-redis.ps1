# ============================================
# Script de Inicio Rápido para EDA con Kafka y Redis (Windows PowerShell)
# ============================================

$ErrorActionPreference = "Stop"

Write-Host "╔══════════════════════════════════════════════════════════════╗"
Write-Host "║                                                              ║"
Write-Host "║  🚀 Configurando EDA con Apache Kafka y Redis              ║"
Write-Host "║                                                              ║"
Write-Host "╚══════════════════════════════════════════════════════════════╝"
Write-Host ""

# Paso 1: Verificar prerrequisitos
Write-Host "📋 Verificando prerrequisitos..." -ForegroundColor Yellow

$dockerInstalled = Get-Command docker -ErrorAction SilentlyContinue
$dotnetInstalled = Get-Command dotnet -ErrorAction SilentlyContinue

if (-not $dockerInstalled) {
    Write-Host "❌ Docker no está instalado" -ForegroundColor Red
    Write-Host "   Instala Docker Desktop desde: https://www.docker.com/products/docker-desktop"
    exit 1
}

if (-not $dotnetInstalled) {
    Write-Host "❌ .NET SDK no está instalado" -ForegroundColor Red
    Write-Host "   Instala .NET 8 desde: https://dotnet.microsoft.com/download"
    exit 1
}

Write-Host "✅ Docker instalado" -ForegroundColor Green
Write-Host "✅ .NET SDK instalado" -ForegroundColor Green
Write-Host ""

# Paso 2: Instalar dependencias NuGet
Write-Host "📦 Instalando paquetes NuGet..." -ForegroundColor Yellow

Set-Location -Path "EDA.API"

$csproj = Get-Content "EDA.API.csproj"

if ($csproj -notmatch "Confluent.Kafka") {
    Write-Host "   Agregando Confluent.Kafka..."
    dotnet add package Confluent.Kafka --version 2.3.0 | Out-Null
}

if ($csproj -notmatch "StackExchange.Redis") {
    Write-Host "   Agregando StackExchange.Redis..."
    dotnet add package StackExchange.Redis --version 2.7.10 | Out-Null
}

Write-Host "✅ Paquetes instalados" -ForegroundColor Green
Write-Host ""

# Paso 3: Configurar Program.cs
Write-Host "⚙️  Configurando archivos..." -ForegroundColor Yellow

if (-not (Test-Path "Program.InMemory.cs") -and (Test-Path "Program.cs")) {
    Write-Host "   Respaldando Program.cs original como Program.InMemory.cs"
    Copy-Item "Program.cs" "Program.InMemory.cs"
}

if (Test-Path "Program.WithKafkaRedis.cs") {
    Write-Host "   Activando configuración con Kafka y Redis"
    Copy-Item "Program.WithKafkaRedis.cs" "Program.cs" -Force
}

Write-Host "✅ Archivos configurados" -ForegroundColor Green
Write-Host ""

Set-Location -Path ".."

# Paso 4: Levantar infraestructura
Write-Host "🐳 Levantando infraestructura con Docker..." -ForegroundColor Yellow
Write-Host "   Esto puede tomar 1-2 minutos la primera vez..."
Write-Host ""

docker-compose -f docker-compose.full.yml up -d

Write-Host ""
Write-Host "⏳ Esperando que los servicios estén listos..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Verificar que Redis está listo
Write-Host "   Verificando Redis..." -NoNewline
$maxRetries = 30
$retry = 0
while ($true) {
    try {
        docker exec eda-redis redis-cli ping 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { break }
    }
    catch { }

    $retry++
    if ($retry -gt $maxRetries) {
        Write-Host " ❌ Redis no arrancó" -ForegroundColor Red
        exit 1
    }
    Start-Sleep -Seconds 1
    Write-Host "." -NoNewline
}
Write-Host " ✅" -ForegroundColor Green

# Verificar que Kafka está listo
Write-Host "   Verificando Kafka..." -NoNewline
$retry = 0
while ($true) {
    try {
        docker exec eda-kafka kafka-broker-api-versions --bootstrap-server localhost:9092 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { break }
    }
    catch { }

    $retry++
    if ($retry -gt $maxRetries) {
        Write-Host " ❌ Kafka no arrancó" -ForegroundColor Red
        exit 1
    }
    Start-Sleep -Seconds 2
    Write-Host "." -NoNewline
}
Write-Host " ✅" -ForegroundColor Green

Write-Host ""
Write-Host "✅ Infraestructura lista" -ForegroundColor Green
Write-Host ""

# Paso 5: Crear topic de Kafka
Write-Host "📝 Creando topic de Kafka..." -ForegroundColor Yellow

docker exec eda-kafka kafka-topics `
    --bootstrap-server localhost:9092 `
    --create `
    --if-not-exists `
    --topic orders-events `
    --partitions 3 `
    --replication-factor 1 2>$null | Out-Null

Write-Host "✅ Topic 'orders-events' creado" -ForegroundColor Green
Write-Host ""

# Paso 6: Compilar la API
Write-Host "🔨 Compilando la API..." -ForegroundColor Yellow

Set-Location -Path "EDA.API"
dotnet build | Out-Null

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ API compilada exitosamente" -ForegroundColor Green
} else {
    Write-Host "❌ Error compilando la API" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "╔══════════════════════════════════════════════════════════════╗"
Write-Host "║                                                              ║"
Write-Host "║  ✅ ¡Configuración completada exitosamente!                ║"
Write-Host "║                                                              ║"
Write-Host "║  Para ejecutar la API:                                       ║"
Write-Host "║    cd EDA.API                                                ║"
Write-Host "║    dotnet run                                                ║"
Write-Host "║                                                              ║"
Write-Host "║  Herramientas visuales:                                      ║"
Write-Host "║    🔍 Kafka UI:        http://localhost:8080                ║"
Write-Host "║    🔴 Redis Commander: http://localhost:8081                ║"
Write-Host "║                        (admin / admin123)                    ║"
Write-Host "║    📖 Swagger:         http://localhost:5200                ║"
Write-Host "║                                                              ║"
Write-Host "║  Ver guía completa:                                          ║"
Write-Host "║    Get-Content README-KAFKA-REDIS.md                         ║"
Write-Host "║                                                              ║"
Write-Host "╚══════════════════════════════════════════════════════════════╝"
Write-Host ""
Write-Host "💡 Tip: Ejecuta 'docker-compose -f docker-compose.full.yml logs -f' para ver logs" -ForegroundColor Cyan
Write-Host ""
