#!/bin/bash

# ============================================
# Script de Inicio Rápido para EDA con Kafka y Redis
# ============================================

set -e  # Detener en caso de error

echo "╔══════════════════════════════════════════════════════════════╗"
echo "║                                                              ║"
echo "║  🚀 Configurando EDA con Apache Kafka y Redis              ║"
echo "║                                                              ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""

# Colores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Paso 1: Verificar prerrequisitos
echo "📋 Verificando prerrequisitos..."

if ! command -v docker &> /dev/null; then
    echo -e "${RED}❌ Docker no está instalado${NC}"
    echo "   Instala Docker Desktop desde: https://www.docker.com/products/docker-desktop"
    exit 1
fi

if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}❌ .NET SDK no está instalado${NC}"
    echo "   Instala .NET 8 desde: https://dotnet.microsoft.com/download"
    exit 1
fi

echo -e "${GREEN}✅ Docker instalado${NC}"
echo -e "${GREEN}✅ .NET SDK instalado${NC}"
echo ""

# Paso 2: Instalar dependencias NuGet
echo "📦 Instalando paquetes NuGet..."

cd EDA.API

if ! grep -q "Confluent.Kafka" EDA.API.csproj; then
    echo "   Agregando Confluent.Kafka..."
    dotnet add package Confluent.Kafka --version 2.3.0 > /dev/null
fi

if ! grep -q "StackExchange.Redis" EDA.API.csproj; then
    echo "   Agregando StackExchange.Redis..."
    dotnet add package StackExchange.Redis --version 2.7.10 > /dev/null
fi

echo -e "${GREEN}✅ Paquetes instalados${NC}"
echo ""

# Paso 3: Configurar Program.cs
echo "⚙️  Configurando archivos..."

if [ ! -f "Program.InMemory.cs" ] && [ -f "Program.cs" ]; then
    echo "   Respaldando Program.cs original como Program.InMemory.cs"
    cp Program.cs Program.InMemory.cs
fi

if [ -f "Program.WithKafkaRedis.cs" ]; then
    echo "   Activando configuración con Kafka y Redis"
    cp Program.WithKafkaRedis.cs Program.cs
fi

echo -e "${GREEN}✅ Archivos configurados${NC}"
echo ""

cd ..

# Paso 4: Levantar infraestructura
echo "🐳 Levantando infraestructura con Docker..."
echo "   Esto puede tomar 1-2 minutos la primera vez..."
echo ""

docker-compose -f docker-compose.full.yml up -d

echo ""
echo "⏳ Esperando que los servicios estén listos..."
sleep 10

# Verificar que Redis está listo
echo -n "   Verificando Redis..."
MAX_RETRIES=30
RETRY=0
while ! docker exec eda-redis redis-cli ping &> /dev/null; do
    RETRY=$((RETRY+1))
    if [ $RETRY -gt $MAX_RETRIES ]; then
        echo -e "${RED}❌ Redis no arrancó${NC}"
        exit 1
    fi
    sleep 1
    echo -n "."
done
echo -e " ${GREEN}✅${NC}"

# Verificar que Kafka está listo
echo -n "   Verificando Kafka..."
RETRY=0
while ! docker exec eda-kafka kafka-broker-api-versions --bootstrap-server localhost:9092 &> /dev/null; do
    RETRY=$((RETRY+1))
    if [ $RETRY -gt $MAX_RETRIES ]; then
        echo -e "${RED}❌ Kafka no arrancó${NC}"
        exit 1
    fi
    sleep 2
    echo -n "."
done
echo -e " ${GREEN}✅${NC}"

echo ""
echo -e "${GREEN}✅ Infraestructura lista${NC}"
echo ""

# Paso 5: Crear topic de Kafka
echo "📝 Creando topic de Kafka..."

docker exec eda-kafka kafka-topics \
    --bootstrap-server localhost:9092 \
    --create \
    --if-not-exists \
    --topic orders-events \
    --partitions 3 \
    --replication-factor 1 &> /dev/null

echo -e "${GREEN}✅ Topic 'orders-events' creado${NC}"
echo ""

# Paso 6: Compilar la API
echo "🔨 Compilando la API..."

cd EDA.API
dotnet build > /dev/null

if [ $? -eq 0 ]; then
    echo -e "${GREEN}✅ API compilada exitosamente${NC}"
else
    echo -e "${RED}❌ Error compilando la API${NC}"
    exit 1
fi

echo ""
echo "╔══════════════════════════════════════════════════════════════╗"
echo "║                                                              ║"
echo "║  ✅ ¡Configuración completada exitosamente!                ║"
echo "║                                                              ║"
echo "║  Para ejecutar la API:                                       ║"
echo "║    cd EDA.API                                                ║"
echo "║    dotnet run                                                ║"
echo "║                                                              ║"
echo "║  Herramientas visuales:                                      ║"
echo "║    🔍 Kafka UI:        http://localhost:8080                ║"
echo "║    🔴 Redis Commander: http://localhost:8081                ║"
echo "║                        (admin / admin123)                    ║"
echo "║    📖 Swagger:         http://localhost:5200                ║"
echo "║                                                              ║"
echo "║  Ver guía completa:                                          ║"
echo "║    cat README-KAFKA-REDIS.md                                 ║"
echo "║                                                              ║"
echo "╚══════════════════════════════════════════════════════════════╝"
echo ""
echo -e "${YELLOW}💡 Tip: Ejecuta 'docker-compose -f docker-compose.full.yml logs -f' para ver logs${NC}"
echo ""
