# Clean Architecture - Docker Setup

Este directorio contiene la configuración de Docker para la arquitectura Clean Architecture.

## Requisitos Previos

- Docker Desktop 20.10+
- Docker Compose 2.0+
- 4GB RAM mínimo disponible para Docker

## Servicios Incluidos

1. **cleanarchitecture-api** - API principal (.NET 8)
2. **sqlserver** - SQL Server 2022 (base de datos principal)
3. **redis** - Redis 7 (caching, opcional)
4. **postgres** - PostgreSQL 16 (alternativa a SQL Server, comentado por defecto)

## Inicio Rápido

### 1. Construir y levantar todos los servicios

```bash
docker-compose up -d --build
```

### 2. Ver logs

```bash
# Todos los servicios
docker-compose logs -f

# Solo la API
docker-compose logs -f cleanarchitecture-api

# Solo la base de datos
docker-compose logs -f sqlserver
```

### 3. Verificar estado

```bash
docker-compose ps
```

### 4. Acceder a la aplicación

- **API**: http://localhost:5001
- **Swagger**: http://localhost:5001/swagger
- **Health Check**: http://localhost:5001/health

### 5. Conectar a las bases de datos

**SQL Server:**
- Host: localhost
- Port: 1433
- User: sa
- Password: YourStrong@Passw0rd
- Database: CleanArchitectureDb

**Redis:**
- Host: localhost
- Port: 6379

## Comandos Útiles

### Detener todos los servicios

```bash
docker-compose down
```

### Detener y eliminar volúmenes (CUIDADO: elimina datos)

```bash
docker-compose down -v
```

### Reconstruir solo la API

```bash
docker-compose up -d --build cleanarchitecture-api
```

### Ejecutar migraciones

```bash
# Desde dentro del contenedor
docker-compose exec cleanarchitecture-api dotnet ef database update

# O desde el host (requiere .NET SDK)
dotnet ef database update --project Infrastructure --startup-project API
```

### Ver recursos utilizados

```bash
docker stats
```

### Acceder al shell del contenedor

```bash
# API
docker-compose exec cleanarchitecture-api /bin/bash

# SQL Server
docker-compose exec sqlserver /bin/bash
```

## Usar PostgreSQL en lugar de SQL Server

1. Editar `docker-compose.yml`:
   - Comentar el servicio `sqlserver`
   - Descomentar el servicio `postgres`

2. Actualizar connection string en el servicio API:
   ```yaml
   - ConnectionStrings__DefaultConnection=Host=postgres;Database=CleanArchitectureDb;Username=postgres;Password=postgres
   ```

3. Reconstruir:
   ```bash
   docker-compose down -v
   docker-compose up -d --build
   ```

## Troubleshooting

### El contenedor de SQL Server no arranca

- Verificar que tienes suficiente RAM (mínimo 2GB para SQL Server)
- Revisar logs: `docker-compose logs sqlserver`

### La API no puede conectar a la base de datos

- Verificar que SQL Server está healthy: `docker-compose ps`
- Esperar 30-40 segundos después del inicio para que SQL Server esté listo
- Verificar logs: `docker-compose logs cleanarchitecture-api`

### Error "port already allocated"

- Cambiar los puertos en `docker-compose.yml`
- O detener el servicio que está usando el puerto

### Limpiar todo y empezar de cero

```bash
docker-compose down -v --remove-orphans
docker system prune -a
docker-compose up -d --build
```

## Desarrollo Local

Para desarrollo con hot reload, puedes montar el código fuente:

1. Descomentar el volumen en `docker-compose.override.yml`
2. Cambiar el ENTRYPOINT a `dotnet watch run`

## Seguridad

**IMPORTANTE**: Las credenciales en estos archivos son solo para desarrollo local.

Para producción:
- Usar variables de entorno desde archivos `.env`
- Usar secrets de Docker o Kubernetes
- Cambiar todas las contraseñas
- Habilitar SSL/TLS
- Configurar firewalls apropiados

## Health Checks

Todos los servicios tienen health checks configurados:

- **API**: `http://localhost:8080/health`
- **SQL Server**: Verifica conexión con sqlcmd
- **Redis**: Comando PING

Ver estado:
```bash
docker inspect --format='{{.State.Health.Status}}' cleanarchitecture-api
```

## Volúmenes Persistentes

Los datos se persisten en volúmenes de Docker:

- `sqlserver-data` - Datos de SQL Server
- `redis-data` - Datos de Redis

Para backup:
```bash
docker run --rm -v sqlserver-data:/data -v $(pwd):/backup alpine tar czf /backup/sqlserver-backup.tar.gz /data
```

## Recursos

- [Docker Documentation](https://docs.docker.com/)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [.NET Docker Images](https://hub.docker.com/_/microsoft-dotnet)
- [SQL Server Docker](https://hub.docker.com/_/microsoft-mssql-server)
