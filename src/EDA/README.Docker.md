# EDA (Event-Driven Architecture) - Docker Setup

Este directorio contiene la configuración de Docker para la arquitectura Event-Driven con Event Sourcing y CQRS.

## Arquitectura

```
┌─────────────┐
│   EDA API   │ ← .NET 8 API con CQRS
└──────┬──────┘
       │
       ├─────────────────┐
       │                 │
       ▼                 ▼
┌─────────────┐   ┌─────────────┐
│    Kafka    │   │    Redis    │
│ Event Store │   │ Read Models │
└─────────────┘   └─────────────┘
       │
       ▼
┌─────────────┐
│  Zookeeper  │
└─────────────┘
```

## Servicios Incluidos

1. **eda-api** - API principal (.NET 8) con CQRS y Event Sourcing
2. **kafka** - Apache Kafka 7.5 (Event Store)
3. **zookeeper** - Apache Zookeeper (requerido por Kafka)
4. **redis** - Redis 7 (Read Models / Projections)
5. **kafka-ui** - UI para monitorear Kafka (opcional, profile: tools)
6. **redis-commander** - UI para Redis (opcional, profile: tools)
7. **kafka-init** - Inicialización de topics (profile: init)

## Requisitos Previos

- Docker Desktop 20.10+
- Docker Compose 2.0+
- 8GB RAM mínimo (Kafka requiere considerable memoria)
- 10GB espacio en disco

## Inicio Rápido

### 1. Iniciar infraestructura y API

```bash
# Iniciar todos los servicios principales
docker-compose up -d

# Ver logs
docker-compose logs -f eda-api
```

### 2. Crear topics de Kafka (primera vez)

```bash
# Ejecutar el servicio de inicialización
docker-compose --profile init up kafka-init

# O crear topics manualmente
docker-compose exec kafka kafka-topics --create --if-not-exists \
  --bootstrap-server localhost:9092 \
  --partitions 3 \
  --replication-factor 1 \
  --topic order-events
```

### 3. Iniciar herramientas de monitoring (opcional)

```bash
# Iniciar Kafka UI y Redis Commander
docker-compose --profile tools up -d

# Acceder a las interfaces
# Kafka UI: http://localhost:8090
# Redis Commander: http://localhost:8091
```

### 4. Verificar estado

```bash
docker-compose ps
```

### 5. Acceder a la aplicación

- **API**: http://localhost:5200
- **Swagger**: http://localhost:5200/swagger
- **Health Check**: http://localhost:5200/health
- **Kafka UI**: http://localhost:8090 (si profile tools está activo)
- **Redis Commander**: http://localhost:8091 (si profile tools está activo)

## Topics de Kafka

Los siguientes topics se crean automáticamente:

1. **order-events** - Eventos del agregado Order (3 particiones)
2. **customer-events** - Eventos del agregado Customer (3 particiones)
3. **product-events** - Eventos del agregado Product (3 particiones)
4. **event-snapshots** - Snapshots para optimizar replay (1 partición)

### Ver topics

```bash
docker-compose exec kafka kafka-topics --list --bootstrap-server localhost:9092
```

### Ver eventos en un topic

```bash
docker-compose exec kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic order-events \
  --from-beginning \
  --property print.key=true \
  --property print.timestamp=true
```

### Describir un topic

```bash
docker-compose exec kafka kafka-topics --describe \
  --bootstrap-server localhost:9092 \
  --topic order-events
```

## Redis - Read Models

Redis almacena las proyecciones (read models) generadas desde los eventos.

### Conectar a Redis

```bash
docker-compose exec redis redis-cli
```

### Ver keys

```bash
# Desde redis-cli
KEYS *

# Ver un order read model
GET order:{order-id}

# Ver todos los orders
KEYS order:*
```

### Limpiar read models

```bash
docker-compose exec redis redis-cli FLUSHALL
```

## Comandos Útiles

### Gestión de servicios

```bash
# Iniciar solo Kafka y dependencias
docker-compose up -d zookeeper kafka redis

# Reiniciar la API
docker-compose restart eda-api

# Ver logs en tiempo real
docker-compose logs -f eda-api kafka redis

# Detener todo
docker-compose down

# Detener y eliminar volúmenes (CUIDADO: elimina eventos!)
docker-compose down -v
```

### Monitoreo de Kafka

```bash
# Ver consumer groups
docker-compose exec kafka kafka-consumer-groups --list --bootstrap-server localhost:9092

# Ver lag de un consumer group
docker-compose exec kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group eda-api-consumer-group \
  --describe

# Ver configuración de un topic
docker-compose exec kafka kafka-configs \
  --bootstrap-server localhost:9092 \
  --entity-type topics \
  --entity-name order-events \
  --describe
```

### Debugging

```bash
# Shell en el contenedor de la API
docker-compose exec eda-api /bin/bash

# Shell en Kafka
docker-compose exec kafka /bin/bash

# Ver métricas de Kafka (JMX)
# Conectar a kafka:9997 con JConsole
```

## Event Sourcing - Replay de Eventos

Para reconstruir read models desde eventos:

```bash
# 1. Limpiar read models en Redis
docker-compose exec redis redis-cli FLUSHALL

# 2. Reiniciar la API (procesará todos los eventos desde el inicio)
docker-compose restart eda-api

# 3. Verificar logs
docker-compose logs -f eda-api
```

## Snapshots

Los snapshots se crean automáticamente cada 100 eventos (configurable).

```bash
# Ver snapshots
docker-compose exec kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic event-snapshots \
  --from-beginning
```

## Performance Tuning

### Kafka

Editar variables de entorno en `docker-compose.yml`:

```yaml
# Aumentar retención
KAFKA_LOG_RETENTION_HOURS: 720  # 30 días

# Aumentar particiones (requiere recrear topics)
KAFKA_NUM_PARTITIONS: 6

# Aumentar memoria JVM
KAFKA_HEAP_OPTS: "-Xmx2G -Xms2G"
```

### Redis

```yaml
# Aumentar memoria máxima
command: redis-server --appendonly yes --maxmemory 2gb
```

## Troubleshooting

### Kafka no arranca

```bash
# Verificar logs
docker-compose logs zookeeper kafka

# Asegurarse que Zookeeper está healthy
docker-compose ps zookeeper

# Limpiar volúmenes y reiniciar
docker-compose down -v
docker-compose up -d
```

### API no puede conectar a Kafka

```bash
# Verificar health de Kafka
docker inspect --format='{{.State.Health.Status}}' eda-kafka

# Esperar 30-60 segundos después del inicio
# Kafka puede tardar en estar listo

# Verificar conectividad
docker-compose exec eda-api ping kafka
```

### Consumer lag muy alto

```bash
# Ver lag
docker-compose exec kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group eda-api-consumer-group \
  --describe

# Aumentar particiones del topic
# Escalar API (múltiples instancias)
```

### Redis out of memory

```bash
# Ver uso de memoria
docker-compose exec redis redis-cli INFO memory

# Limpiar cache
docker-compose exec redis redis-cli FLUSHDB

# Aumentar maxmemory en docker-compose.yml
```

## Seguridad

**IMPORTANTE**: Esta configuración es solo para desarrollo local.

Para producción:
- Habilitar SASL/SSL en Kafka
- Configurar ACLs en Kafka
- Usar passwords en Redis
- Implementar autenticación en Kafka UI
- Usar secrets management (Vault, AWS Secrets Manager)
- Network policies en Kubernetes

## Monitoring en Producción

Herramientas recomendadas:
- **Prometheus + Grafana** - Métricas
- **Kafka Exporter** - Exportar métricas de Kafka
- **Redis Exporter** - Exportar métricas de Redis
- **Elastic APM** - Application Performance Monitoring
- **Zipkin/Jaeger** - Distributed tracing

## Backups

### Backup de Kafka (eventos)

```bash
# Los eventos están en el volumen kafka-data
docker run --rm -v eda_kafka-data:/data -v $(pwd):/backup alpine \
  tar czf /backup/kafka-backup-$(date +%Y%m%d).tar.gz /data
```

### Backup de Redis (read models)

```bash
# RDB snapshot
docker-compose exec redis redis-cli SAVE

# Copiar snapshot
docker cp eda-redis:/data/dump.rdb ./redis-backup-$(date +%Y%m%d).rdb
```

## Recursos

- [Apache Kafka Documentation](https://kafka.apache.org/documentation/)
- [Redis Documentation](https://redis.io/documentation)
- [Event Sourcing Pattern](https://microservices.io/patterns/data/event-sourcing.html)
- [CQRS Pattern](https://microservices.io/patterns/data/cqrs.html)
- [Kafka UI](https://github.com/provectus/kafka-ui)

## Profiles

- **default**: API, Kafka, Zookeeper, Redis
- **tools**: + Kafka UI, Redis Commander
- **init**: Kafka topic initialization

```bash
# Iniciar con todas las herramientas
docker-compose --profile tools --profile init up -d
```
