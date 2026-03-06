#!/bin/bash

# Cleanup Script - Limpia todos los contenedores, volúmenes e imágenes

set -e

echo "============================================"
echo "Cleanup - Software Architecture"
echo "============================================"
echo ""
echo "ADVERTENCIA: Esto eliminará:"
echo "  - Todos los contenedores"
echo "  - Todos los volúmenes (datos de bases de datos)"
echo "  - Redes Docker"
echo ""
read -p "¿Estás seguro? (y/N): " confirm

if [ "$confirm" != "y" ] && [ "$confirm" != "Y" ]; then
    echo "Cancelado."
    exit 0
fi

echo ""
echo "Deteniendo contenedores..."
docker-compose down

echo ""
echo "Eliminando volúmenes..."
docker-compose down -v

echo ""
echo "Eliminando redes huérfanas..."
docker network prune -f

echo ""
echo "¿Eliminar también las imágenes Docker? (y/N): "
read -p "" remove_images

if [ "$remove_images" = "y" ] || [ "$remove_images" = "Y" ]; then
    echo "Eliminando imágenes..."
    docker-compose down --rmi all -v
fi

echo ""
echo "✓ Cleanup completado!"
echo ""
echo "Para volver a configurar, ejecuta: ./scripts/setup.sh"
