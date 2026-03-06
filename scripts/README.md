# Scripts de Utilidad

Esta carpeta contiene scripts para facilitar el desarrollo y mantenimiento del repositorio.

## Scripts Disponibles

### `setup.sh`
Script de configuración inicial. Verifica prerequisitos y levanta la infraestructura.

**Uso:**
```bash
./scripts/setup.sh
```

**Funcionalidades:**
- Verifica Docker, Docker Compose y .NET SDK
- Permite seleccionar qué arquitectura configurar (Clean, DDD, EDA, o todas)
- Descarga imágenes y levanta servicios
- Muestra URLs de acceso

---

### `start-all.sh`
Inicia todas las arquitecturas simultáneamente.

**Uso:**
```bash
./scripts/start-all.sh
```

**Puertos:**
- Clean Architecture: http://localhost:5001
- DDD: http://localhost:5100
- EDA: http://localhost:5200

---

### `cleanup.sh`
Limpia todos los contenedores, volúmenes y opcionalmente las imágenes.

**Uso:**
```bash
./scripts/cleanup.sh
```

**ADVERTENCIA:** Elimina todos los datos de bases de datos. Úsalo con precaución.

---

### `run-tests.sh`
Ejecuta todos los tests del repositorio (95+ tests).

**Uso:**
```bash
./scripts/run-tests.sh
```

**Tests ejecutados:**
- Clean Architecture: Domain Tests + Integration Tests
- DDD: Domain Tests + Integration Tests
- EDA: Unit Tests + Integration Tests

---

### `build-all.sh`
Compila todas las soluciones en modo Release.

**Uso:**
```bash
./scripts/build-all.sh
```

**Soluciones compiladas:**
- CleanArchitecture.sln
- DDD.sln
- EDA.sln

---

## Permisos

Asegúrate de dar permisos de ejecución a los scripts:

```bash
chmod +x scripts/*.sh
```

## Requisitos

- **Docker Desktop**: https://www.docker.com/products/docker-desktop
- **Docker Compose**: Incluido con Docker Desktop
- **.NET 8 SDK** (opcional, solo para build y tests): https://dotnet.microsoft.com/download/dotnet/8.0

## Ejemplos de Uso

### Setup Completo
```bash
# Configurar todas las arquitecturas
./scripts/setup.sh
# Seleccionar opción 4 (Todas)
```

### Solo Clean Architecture
```bash
./scripts/setup.sh
# Seleccionar opción 1 (Clean Architecture)
```

### Build y Test
```bash
# Compilar todo
./scripts/build-all.sh

# Ejecutar todos los tests
./scripts/run-tests.sh
```

### Limpieza Total
```bash
# Limpiar todo (contenedores + volúmenes + imágenes)
./scripts/cleanup.sh
```

## Solución de Problemas

### "Permission denied"
```bash
chmod +x scripts/*.sh
```

### "Docker no está instalado"
Instalar Docker Desktop desde https://www.docker.com/products/docker-desktop

### ".NET SDK no está instalado"
Instalar desde https://dotnet.microsoft.com/download/dotnet/8.0

### Puertos en uso
Si los puertos 5001, 5100 o 5200 están en uso, detén los servicios que los estén utilizando o modifica los puertos en `docker-compose.yml`.

## Notas

- Los scripts están optimizados para Linux/macOS
- Para Windows, usa Git Bash o WSL2
- Los datos se persisten en volúmenes Docker
- Para desarrollo con hot reload, usa `dotnet watch run` en lugar de Docker
