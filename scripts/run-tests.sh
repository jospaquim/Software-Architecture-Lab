#!/bin/bash

# Run Tests - Ejecuta todos los tests del repositorio

set -e

echo "============================================"
echo "Running All Tests"
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

FAILED=0

echo -e "${YELLOW}Clean Architecture Tests:${NC}"
echo "  - Domain Tests..."
dotnet test tests/CleanArchitecture.Domain.Tests/CleanArchitecture.Domain.Tests.csproj --nologo -v quiet || FAILED=1

echo "  - Integration Tests..."
dotnet test tests/CleanArchitecture.IntegrationTests/CleanArchitecture.IntegrationTests.csproj --nologo -v quiet || FAILED=1

echo ""
echo -e "${YELLOW}DDD Tests:${NC}"
echo "  - Domain Tests..."
dotnet test tests/DDD.Sales.Domain.Tests/DDD.Sales.Domain.Tests.csproj --nologo -v quiet || FAILED=1

echo "  - Integration Tests..."
dotnet test tests/DDD.Sales.IntegrationTests/DDD.Sales.IntegrationTests.csproj --nologo -v quiet || FAILED=1

echo ""
echo -e "${YELLOW}EDA Tests:${NC}"
echo "  - Unit Tests..."
dotnet test tests/EDA.Tests/EDA.Tests.csproj --nologo -v quiet || FAILED=1

echo "  - Integration Tests..."
dotnet test tests/EDA.IntegrationTests/EDA.IntegrationTests.csproj --nologo -v quiet || FAILED=1

echo ""
echo "============================================"
if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}✓ All tests passed!${NC}"
else
    echo -e "${RED}✗ Some tests failed${NC}"
    exit 1
fi
echo "============================================"
