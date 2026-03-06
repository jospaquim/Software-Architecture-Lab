#!/bin/bash

# Build All - Compila todas las soluciones

set -e

echo "============================================"
echo "Building All Solutions"
echo "============================================"
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo -e "${RED}✗ .NET SDK no está instalado${NC}"
    echo "  Instalar desde: https://dotnet.microsoft.com/download/dotnet/8.0"
    exit 1
fi

echo -e "${YELLOW}Building Clean Architecture...${NC}"
dotnet build src/CleanArchitecture/CleanArchitecture.sln --configuration Release

echo ""
echo -e "${YELLOW}Building DDD...${NC}"
dotnet build src/DDD/DDD.sln --configuration Release

echo ""
echo -e "${YELLOW}Building EDA...${NC}"
dotnet build src/EDA/EDA.sln --configuration Release

echo ""
echo "============================================"
echo -e "${GREEN}✓ All solutions built successfully!${NC}"
echo "============================================"
