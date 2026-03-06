#  Guía de Inicio Rápido: EDA con Kafka y Redis

Esta guía te llevará paso a paso desde cero hasta tener una aplicación EDA funcionando con Apache Kafka y Redis.

##  Prerrequisitos

- Docker Desktop instalado
- .NET 8 SDK instalado
- Un editor de código (VS Code, Visual Studio, Rider)

## ‍️ Inicio Rápido (5 minutos)

### Paso 1: Instalar dependencias NuGet

```bash
cd src/EDA/EDA.API

# Instalar Kafka client
dotnet add package Confluent.Kafka --version 2.3.0

# Instalar Redis client
dotnet add package StackExchange.Redis --version 2.7.10
```

### Paso 2: Cambiar a la configuración con Kafka y Redis

```bash
# Renombrar el Program.cs actual (usa in-memory)
mv Program.cs Program.InMemory.cs

# Activar el Program.cs con Kafka y Redis
mv Program.WithKafkaRedis.cs Program.cs
```

### Paso 3: Levantar infraestructura con Docker

```bash
cd ..  # Regresar a src/EDA

# Levantar Redis, Kafka, Zookeeper y herramientas visuales
docker-compose -f docker-compose.full.yml up -d

# Ver logs para confirmar que todo está corriendo
docker-compose -f docker-compose.full.yml logs -f
```

Espera a ver estos mensajes:
-  `eda-redis | Ready to accept connections`
-  `eda-kafka | started (kafka.server.KafkaServer)`
-  `eda-kafka-ui | Started`

Presiona `Ctrl+C` para salir de los logs.

### Paso 4: Verificar que todo está corriendo

```bash
docker-compose -f docker-compose.full.yml ps
```

Deberías ver:
-  eda-redis (puerto 6379)
-  eda-kafka (puerto 9092)
-  eda-zookeeper (puerto 2181)
-  eda-kafka-ui (puerto 8080)
-  eda-redis-commander (puerto 8081)

### Paso 5: Ejecutar la API

```bash
cd EDA.API
dotnet run
```

Deberías ver:
```
 Redis configured successfully
 Kafka configured successfully
 Projections subscribed successfully
 Event-Driven Architecture (EDA) API
   with Apache Kafka & Redis
```

### Paso 6: Probar la API

Abre otra terminal y ejecuta:

```bash
# Crear una orden
curl -X POST http://localhost:5200/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "123e4567-e89b-12d3-a456-426614174000"
  }'

# Respuesta: {"orderId": "abc-def-ghi-..."}
# Copia el orderId para los siguientes comandos
```

```bash
# Agregar un item (reemplaza {orderId})
curl -X POST http://localhost:5200/api/v1/orders/{orderId}/items \
  -H "Content-Type: application/json" \
  -d '{
    "productId": "550e8400-e29b-41d4-a716-446655440000",
    "productName": "Laptop Dell XPS 15",
    "unitPrice": 1500.00,
    "quantity": 2
  }'
```

```bash
# Confirmar la orden
curl -X POST http://localhost:5200/api/v1/orders/{orderId}/confirm
```

```bash
# Consultar la orden (Read Model desde Redis)
curl http://localhost:5200/api/v1/orders/{orderId}
```

### Paso 7: Ver eventos en Kafka UI

1. Abre http://localhost:8080 en tu navegador
2. Click en "Topics" → "orders-events"
3. Click en "Messages"
4. Verás todos los eventos:
   - OrderCreatedEvent
   - ItemAddedEvent
   - OrderConfirmedEvent

### Paso 8: Ver Read Model en Redis Commander

1. Abre http://localhost:8081 en tu navegador
2. Login: admin / admin123
3. Busca la clave: `order:{tu-orderId}`
4. Verás el JSON del Read Model

##  ¿Qué acabas de hacer?

### Write Side (Comandos)
1. Enviaste un comando POST → Create Order
2. La API creó un `OrderAggregate` y generó eventos
3. Los eventos se guardaron en **Kafka** (Event Store)
4. Los eventos se publicaron en **Kafka** (Event Bus)

### Read Side (Consultas)
5. La `OrderProjection` escuchó los eventos desde Kafka
6. La proyección actualizó el Read Model en **Redis**
7. Cuando hiciste GET, consultaste directamente **Redis** (ultra rápido)

### Event Sourcing
8. El estado de la orden se reconstruye desde eventos almacenados en Kafka
9. Puedes hacer "replay" de eventos en cualquier momento

##  Explorando las herramientas

### Kafka UI (http://localhost:8080)

**Ver todos los eventos:**
1. Topics → orders-events → Messages
2. Puedes ver el JSON de cada evento
3. Puedes ver a qué partición fue cada evento
4. Puedes ver el offset (posición) de cada evento

**Crear un nuevo topic:**
1. Topics → Add a Topic
2. Nombre: "customer-events"
3. Partitions: 3

**Ver consumer groups:**
1. Consumers
2. Verás "eda-order-service" (tu API)
3. Verás el "lag" (eventos pendientes de procesar)

### Redis Commander (http://localhost:8081)

**Ver todas las órdenes:**
- Busca claves que empiecen con `order:`
- Click en una clave para ver el JSON

**Ver índices:**
- `customer:orders:{customerId}` → Set con IDs de órdenes del cliente
- `status:orders:Draft` → Set con IDs de órdenes en estado Draft
- `all:orders` → Sorted Set con todas las órdenes ordenadas por fecha

**Ejecutar comandos Redis:**
1. Click en "CLI"
2. Ejecuta: `KEYS order:*` → Ver todas las órdenes
3. Ejecuta: `GET order:{id}` → Ver una orden específica
4. Ejecuta: `SMEMBERS status:orders:Confirmed` → Ver órdenes confirmadas

##  Casos de prueba avanzados

### Test 1: Eventual Consistency

```bash
# 1. Crear orden
RESPONSE=$(curl -s -X POST http://localhost:5200/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{"customerId": "123e4567-e89b-12d3-a456-426614174000"}')

ORDER_ID=$(echo $RESPONSE | jq -r '.orderId')

# 2. Inmediatamente consultar (puede no existir todavía)
curl http://localhost:5200/api/v1/orders/$ORDER_ID

# Esperar 100ms y consultar nuevamente (ahora sí debería existir)
sleep 0.1
curl http://localhost:5200/api/v1/orders/$ORDER_ID
```

### Test 2: Event Sourcing - Ver historia de eventos

```bash
# Ver todos los eventos de una orden
curl http://localhost:5200/api/v1/orders/{orderId}/events
```

Verás todos los eventos en orden:
1. OrderCreatedEvent (v1)
2. ItemAddedEvent (v2)
3. ItemAddedEvent (v3)
4. OrderConfirmedEvent (v4)

### Test 3: Múltiples consumidores

```bash
# Terminal 1: Ejecutar API (puerto 5200)
cd src/EDA/EDA.API
dotnet run

# Terminal 2: Ejecutar segunda instancia (puerto 5201)
cd src/EDA/EDA.API
dotnet run --urls http://localhost:5201
```

Ambas instancias procesarán eventos de diferentes particiones de Kafka.

### Test 4: Replay de eventos

```bash
# En Redis Commander, borra una orden:
# CLI: DEL order:{orderId}

# En tu API, implementa un endpoint de replay:
curl -X POST http://localhost:5200/api/v1/orders/{orderId}/replay
```

La orden se reconstruirá desde los eventos en Kafka.

##  Troubleshooting

### Problema: "Connection refused" a Redis

**Verificar que Redis está corriendo:**
```bash
docker-compose -f docker-compose.full.yml ps redis
```

**Ver logs de Redis:**
```bash
docker-compose -f docker-compose.full.yml logs redis
```

**Probar conexión manual:**
```bash
docker exec -it eda-redis redis-cli ping
# Debería responder: PONG
```

### Problema: "Connection refused" a Kafka

**Verificar que Kafka está corriendo:**
```bash
docker-compose -f docker-compose.full.yml ps kafka
```

**Ver logs de Kafka:**
```bash
docker-compose -f docker-compose.full.yml logs kafka | tail -100
```

**Crear topic manualmente:**
```bash
docker exec -it eda-kafka kafka-topics \
  --bootstrap-server localhost:9092 \
  --create \
  --topic orders-events \
  --partitions 3 \
  --replication-factor 1
```

### Problema: Eventos no se procesan

**Ver consumer lag:**
1. Kafka UI → Consumers → eda-order-service
2. Si "Lag" > 0, hay eventos pendientes

**Ver logs de la API:**
```bash
# Buscar mensajes como:
#  Event OrderCreatedEvent processed
#  Error processing event
```

**Reiniciar consumer:**
```bash
# Detener API (Ctrl+C)
# Eliminar offsets
docker exec -it eda-kafka kafka-consumer-groups \
  --bootstrap-server localhost:9092 \
  --group eda-order-service \
  --reset-offsets \
  --to-earliest \
  --topic orders-events \
  --execute

# Reiniciar API
dotnet run
```

### Problema: Redis se queda sin memoria

**Verificar uso de memoria:**
```bash
docker exec -it eda-redis redis-cli INFO memory
```

**Limpiar todas las claves:**
```bash
docker exec -it eda-redis redis-cli FLUSHALL
```

**Cambiar política de eviction:**
En `docker-compose.full.yml`, cambia:
```yaml
--maxmemory 256mb              # Aumentar a 512mb
--maxmemory-policy allkeys-lru # Política de eviction
```

##  Limpiar todo

```bash
# Detener contenedores
docker-compose -f docker-compose.full.yml down

# Detener y borrar volúmenes (borra todos los datos)
docker-compose -f docker-compose.full.yml down -v

# Verificar que todo se borró
docker volume ls | grep eda
```

##  Monitoreo en producción

### Métricas importantes de Kafka

1. **Consumer Lag**: Eventos pendientes de procesar
   - Crítico si > 1000
   - Alertar si crece consistentemente

2. **Partitions**: Número de particiones por topic
   - Más partitions = más paralelismo
   - Recomendado: 3-10 partitions por topic

3. **Replication Factor**: Copias de cada partition
   - Desarrollo: 1
   - Producción: 3 (alta disponibilidad)

### Métricas importantes de Redis

1. **Used Memory**: Memoria usada
   - Crítico si > 80% del límite
   - Alertar si crece > 10% por hora

2. **Hit Rate**: % de consultas que encuentran la clave
   - Bueno: > 90%
   - Malo: < 70%

3. **Connected Clients**: Número de conexiones
   - Normal: 10-100
   - Alto: > 1000 (puede indicar leak de conexiones)

##  Próximos pasos

1. **Implementar más eventos:**
   - OrderDeliveredEvent
   - OrderRefundedEvent
   - PaymentProcessedEvent

2. **Crear más proyecciones:**
   - CustomerOrderSummaryProjection
   - OrderStatisticsProjection
   - InventoryProjection

3. **Agregar más bounded contexts:**
   - Catalog (productos)
   - Inventory (stock)
   - Shipping (envíos)

4. **Implementar Saga pattern:**
   - Para procesos que involucran múltiples aggregates
   - Ejemplo: Proceso de checkout completo

5. **Agregar monitoring:**
   - Prometheus + Grafana
   - Alertas para consumer lag
   - Dashboard de métricas

##  Recursos adicionales

- [Documentación completa](../../docs/eda/REDIS-KAFKA-GUIDE.md)
- [Kafka Documentation](https://kafka.apache.org/documentation/)
- [Redis Documentation](https://redis.io/docs/)
- [Event Sourcing by Martin Fowler](https://martinfowler.com/eaaDev/EventSourcing.html)
- [CQRS Journey](https://docs.microsoft.com/en-us/previous-versions/msp-n-p/jj554200(v=pandp.10))

---

¡Felicidades!  Ahora tienes una arquitectura EDA completa con Kafka y Redis funcionando.
