#!/bin/bash

# Start All - Levanta todas las arquitecturas

set -e

echo "============================================"
echo "Iniciando todas las arquitecturas..."
echo "============================================"
echo ""

# Start all services
docker-compose --profile all up -d

echo ""
echo "Servicios iniciados:"
echo ""
echo "Clean Architecture: http://localhost:5001"
echo "DDD:                http://localhost:5100"
echo "EDA:                http://localhost:5200"
echo ""
echo "Para ver logs:"
echo "  docker-compose logs -f cleanarchitecture-api"
echo "  docker-compose logs -f ddd-sales-api"
echo "  docker-compose logs -f eda-api"
echo ""
echo "Para detener todos: docker-compose down"
