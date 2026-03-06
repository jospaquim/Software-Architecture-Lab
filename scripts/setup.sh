#!/bin/bash

# Setup Script - Software Architecture Repository
# Este script configura el entorno de desarrollo local

set -e

echo "===================================="
echo "Software Architecture - Setup"
echo "===================================="
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check prerequisites
echo "Verificando prerequisitos..."

# Check Docker
if ! command -v docker &> /dev/null; then
    echo -e "${RED}✗ Docker no está instalado${NC}"
    echo "  Instalar desde: https://www.docker.com/products/docker-desktop"
    exit 1
else
    echo -e "${GREEN}✓ Docker instalado${NC}"
fi

# Check Docker Compose
if ! command -v docker-compose &> /dev/null; then
    echo -e "${RED}✗ Docker Compose no está instalado${NC}"
    exit 1
else
    echo -e "${GREEN}✓ Docker Compose instalado${NC}"
fi

# Check .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo -e "${YELLOW}⚠ .NET SDK no está instalado (opcional para desarrollo)${NC}"
    echo "  Instalar desde: https://dotnet.microsoft.com/download/dotnet/8.0"
else
    DOTNET_VERSION=$(dotnet --version)
    echo -e "${GREEN}✓ .NET SDK instalado (version $DOTNET_VERSION)${NC}"
fi

echo ""
echo "===================================="
echo "Selecciona qué arquitectura configurar:"
echo "===================================="
echo "1) Clean Architecture"
echo "2) DDD (Domain-Driven Design)"
echo "3) EDA (Event-Driven Architecture)"
echo "4) Todas"
echo ""
read -p "Opción (1-4): " option

case $option in
    1)
        PROFILE="clean"
        echo -e "${GREEN}Configurando Clean Architecture...${NC}"
        ;;
    2)
        PROFILE="ddd"
        echo -e "${GREEN}Configurando DDD...${NC}"
        ;;
    3)
        PROFILE="eda"
        echo -e "${GREEN}Configurando EDA...${NC}"
        ;;
    4)
        PROFILE="all"
        echo -e "${GREEN}Configurando todas las arquitecturas...${NC}"
        ;;
    *)
        echo -e "${RED}Opción inválida${NC}"
        exit 1
        ;;
esac

echo ""
echo "Descargando imágenes Docker..."
if [ "$PROFILE" = "all" ]; then
    docker-compose pull
else
    docker-compose --profile $PROFILE pull
fi

echo ""
echo "Levantando servicios..."
if [ "$PROFILE" = "all" ]; then
    docker-compose up -d
else
    docker-compose --profile $PROFILE up -d
fi

echo ""
echo "Esperando que los servicios estén listos..."
sleep 10

echo ""
echo -e "${GREEN}===================================="
echo "Setup completado! ✓"
echo "====================================${NC}"
echo ""

if [ "$PROFILE" = "clean" ] || [ "$PROFILE" = "all" ]; then
    echo "Clean Architecture:"
    echo "  - API: http://localhost:5001"
    echo "  - Swagger: http://localhost:5001/swagger"
    echo "  - Health: http://localhost:5001/health"
    echo ""
fi

if [ "$PROFILE" = "ddd" ] || [ "$PROFILE" = "all" ]; then
    echo "DDD:"
    echo "  - API: http://localhost:5100"
    echo "  - Swagger: http://localhost:5100/swagger"
    echo "  - Health: http://localhost:5100/health"
    echo ""
fi

if [ "$PROFILE" = "eda" ] || [ "$PROFILE" = "all" ]; then
    echo "EDA:"
    echo "  - API: http://localhost:5200"
    echo "  - Swagger: http://localhost:5200/swagger"
    echo "  - Health: http://localhost:5200/health"
    echo "  - Kafka UI: http://localhost:8090 (ejecuta: docker-compose --profile eda --profile tools up -d)"
    echo ""
fi

echo "Para ver logs: docker-compose logs -f"
echo "Para detener: docker-compose down"
echo "Para limpiar todo: docker-compose down -v"
