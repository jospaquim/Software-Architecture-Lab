# Observability - Monitoring, Metrics & Distributed Tracing

## Índice

1. [Introducción](#introducción)
2. [Arquitectura del Stack](#arquitectura-del-stack)
3. [Los 3 Pilares de Observability](#los-3-pilares-de-observability)
4. [OpenTelemetry Integration](#opentelemetry-integration)
5. [Prometheus - Métricas](#prometheus---métricas)
6. [Grafana - Visualización](#grafana---visualización)
7. [Jaeger - Distributed Tracing](#jaeger---distributed-tracing)
8. [Quick Start](#quick-start)
9. [Métricas Disponibles](#métricas-disponibles)
10. [Dashboards](#dashboards)
11. [Alerting (Futuro)](#alerting-futuro)
12. [Best Practices](#best-practices)
13. [Troubleshooting](#troubleshooting)

---

## Introducción

La **Observability** (observabilidad) es la capacidad de entender el estado interno de un sistema basándose en sus salidas externas. En sistemas distribuidos modernos, no basta con hacer logging - necesitas **métricas**, **trazas distribuidas** y **logs** trabajando juntos.

### ¿Por qué Observability?

**Sin Observability**:
-  No sabes cuándo hay problemas hasta que los usuarios se quejan
-  Debugging de errores es como buscar una aguja en un pajar
-  No puedes medir el rendimiento real de tu aplicación
-  No tienes visibilidad en sistemas distribuidos

**Con Observability**:
-  **Proactivo**: Detectas problemas antes que los usuarios
-  **Debugging rápido**: Encuentras la raíz del problema en minutos, no horas
-  **Data-driven decisions**: Optimizas basándote en datos reales
-  **Visibilidad total**: Entiendes el flujo completo de requests en múltiples servicios

---

## Arquitectura del Stack

Este proyecto implementa un stack de observability completo usando **OpenTelemetry** como protocolo estándar:

```
┌─────────────────────────────────────────────────────────────┐
│                    APLICACIONES                             │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │   Clean      │  │     DDD      │  │     EDA      │      │
│  │Architecture  │  │  Sales API   │  │     API      │      │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘      │
│         │                  │                  │              │
│         │ OpenTelemetry    │                  │              │
│         └──────────────────┴──────────────────┘              │
│                           │                                  │
└───────────────────────────┼──────────────────────────────────┘
                            │
            ┌───────────────┼───────────────┐
            │               │               │
            ▼               ▼               ▼
     ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
     │ PROMETHEUS  │ │   JAEGER    │ │  Serilog    │
     │  (Metrics)  │ │  (Traces)   │ │   (Logs)    │
     └──────┬──────┘ └──────┬──────┘ └─────────────┘
            │               │
            └───────┬───────┘
                    │
                    ▼
             ┌─────────────┐
             │   GRAFANA   │
             │(Visualization)│
             └─────────────┘
```

### Componentes

| Componente | Propósito | Puerto | URL |
|-----------|-----------|---------|-----|
| **Prometheus** | Recolección y almacenamiento de métricas | 9090 | http://localhost:9090 |
| **Grafana** | Visualización de métricas y dashboards | 3000 | http://localhost:3000 |
| **Jaeger** | Distributed tracing (OTLP receiver) | 16686<br>4317 (OTLP) | http://localhost:16686 |
| **Node Exporter** | Métricas del sistema (CPU, RAM, Disk) | 9100 | http://localhost:9100 |

---

## Los 3 Pilares de Observability

### 1. **Logs** 
**Qué**: Eventos discretos que ocurren en tu aplicación
**Cuándo**: Debugging de errores específicos, auditoría
**Implementación**: Serilog (ya implementado)

```csharp
Log.Information("Order {OrderId} created for customer {CustomerId}", orderId, customerId);
Log.Warning("Payment gateway timeout for order {OrderId}", orderId);
Log.Error(ex, "Failed to process order {OrderId}", orderId);
```

### 2. **Metrics** 
**Qué**: Valores numéricos agregados en el tiempo
**Cuándo**: Monitoreo de salud, alertas, capacidad
**Implementación**: OpenTelemetry + Prometheus

```
http_server_request_duration_seconds{method="GET",route="/api/orders",status_code="200"}
dotnet_gc_collection_count_total{generation="2"}
process_cpu_seconds_total
```

### 3. **Traces** 
**Qué**: Seguimiento de una request a través de múltiples servicios
**Cuándo**: Debugging de latencia, entender flujos distribuidos
**Implementación**: OpenTelemetry + Jaeger

```
TraceID: 5f9c3d8e7b2a1c4f
├─ Span: HTTP GET /api/orders/123 (50ms)
│  ├─ Span: Repository.GetOrderAsync (30ms)
│  │  └─ Span: SQL SELECT * FROM Orders (25ms)
│  └─ Span: Mapper.Map<OrderDto> (5ms)
```

---

## OpenTelemetry Integration

**OpenTelemetry** es el estándar open-source para instrumentación de aplicaciones. Reemplaza a múltiples SDKs propietarios con una API unificada.

### Configuración en Program.cs

Todos los 3 proyectos (CleanArchitecture, DDD, EDA) tienen OpenTelemetry configurado:

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService(serviceName: "CleanArchitecture.API", serviceVersion: "1.0.0")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = builder.Environment.EnvironmentName,
            ["host.name"] = Environment.MachineName
        }))
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()      // HTTP request metrics
        .AddHttpClientInstrumentation()      // HttpClient metrics
        .AddRuntimeInstrumentation()         // .NET runtime metrics
        .AddProcessInstrumentation()         // Process metrics
        .AddPrometheusExporter())            // Export to Prometheus
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()      // HTTP request traces
        .AddHttpClientInstrumentation()      // HttpClient traces
        .AddSqlClientInstrumentation()       // SQL traces
        .AddOtlpExporter(options =>          // Export to Jaeger
        {
            options.Endpoint = new Uri("http://localhost:4317");
        }));
```

### Métricas Instrumentadas Automáticamente

| Instrumentación | Métricas Generadas |
|----------------|-------------------|
| **AspNetCore** | `http_server_request_duration_seconds`<br>`http_server_active_requests`<br>`http_server_request_duration_seconds_bucket` |
| **HttpClient** | `http_client_request_duration_seconds`<br>`http_client_active_requests` |
| **Runtime** | `dotnet_gc_collection_count_total`<br>`dotnet_gc_memory_total_available_bytes`<br>`dotnet_threadpool_thread_count` |
| **Process** | `process_cpu_seconds_total`<br>`process_working_set_bytes`<br>`process_num_threads` |
| **SqlClient** | `db_client_operation_duration_seconds`<br>Query traces con statement |

---

## Prometheus - Métricas

### ¿Qué es Prometheus?

Prometheus es un sistema de monitoreo y base de datos de series temporales. **Scrapes** (recolecta) métricas de endpoints HTTP.

### Configuración (prometheus.yml)

```yaml
scrape_configs:
  - job_name: 'clean-architecture-api'
    scrape_interval: 10s
    metrics_path: '/metrics'
    static_configs:
      - targets: ['clean-api:8080']
```

### Prometheus Query Language (PromQL)

**Request Rate** (requests per second):
```promql
rate(http_server_request_duration_seconds_count[1m])
```

**P99 Latency**:
```promql
histogram_quantile(0.99,
  sum(rate(http_server_request_duration_seconds_bucket[5m])) by (le)
)
```

**Error Rate** (%):
```promql
(sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m]))
/
sum(rate(http_server_request_duration_seconds_count[5m]))) * 100
```

### Explorando Prometheus

1. **Abrir Prometheus UI**: http://localhost:9090
2. **Ver targets**: http://localhost:9090/targets (verifica que las APIs estén UP)
3. **Ejecutar queries**: Usa el Expression Browser
4. **Ver gráficas**: Tab "Graph"

---

## Grafana - Visualización

### Acceso

- **URL**: http://localhost:3000
- **Usuario**: `admin`
- **Password**: `admin` (cambiar en primer login)

### Datasources Pre-Configurados

Grafana viene con datasources auto-configurados:
- **Prometheus**: Para métricas
- **Jaeger**: Para traces

### Dashboards Incluidos

#### 1. **ASP.NET Core API Metrics**
Dashboard completo para las 3 APIs:
- **Request Rate**: Requests/segundo por servicio
- **Response Time**: P50, P90, P99 latencies
- **HTTP Status Codes**: Distribución de 2xx, 4xx, 5xx
- **Error Rate**: % de errores 5xx
- **Total Requests**: Counter de requests
- **.NET Runtime**: GC collections, memory usage
- **CPU Usage**: % de CPU por servicio
- **Request Duration Heatmap**: Distribución de latencias

#### 2. **System Metrics - Infrastructure**
Métricas del sistema vía Node Exporter:
- **CPU Usage**: % de uso del CPU
- **Memory Usage**: % de memoria usada
- **Disk I/O**: Read/Write bytes/sec
- **Network I/O**: Receive/Transmit bytes/sec
- **System Load**: Load average
- **Uptime**: Tiempo desde el último boot
- **Disk Space**: Uso por mount point

### Crear Dashboards Personalizados

1. En Grafana: **Create → Dashboard**
2. **Add panel**
3. Selecciona datasource: **Prometheus**
4. Escribe query PromQL
5. Configura visualización
6. **Save dashboard**

**Ejemplo de Panel**:
```json
{
  "title": "Requests por Endpoint",
  "targets": [{
    "expr": "sum(rate(http_server_request_duration_seconds_count[1m])) by (route)"
  }],
  "type": "timeseries"
}
```

---

## Jaeger - Distributed Tracing

### ¿Qué es Distributed Tracing?

Cuando un request pasa por múltiples servicios/componentes, un **trace** te permite ver el recorrido completo:

```
TraceID: abc123
├─ HTTP GET /api/orders/456 [150ms]
│  ├─ OrdersController.GetOrder [145ms]
│  │  ├─ OrderService.GetOrderAsync [120ms]
│  │  │  ├─ OrderRepository.FindByIdAsync [100ms]
│  │  │  │  └─ SQL Query [90ms] ← Aquí está el bottleneck!
│  │  │  └─ EnrichWithCustomerData [15ms]
│  │  └─ MapToDto [5ms]
│  └─ Serialize JSON [5ms]
```

### Acceso a Jaeger UI

**URL**: http://localhost:16686

### Buscando Traces

1. **Service**: Selecciona el servicio (CleanArchitecture.API, DDD.Sales.API, EDA.API)
2. **Operation**: Filtrar por operación (GET /api/orders, POST /api/orders)
3. **Lookback**: Timeframe (last hour, last 15min, etc.)
4. **Click "Find Traces"**

### Anatomía de un Trace

- **TraceID**: Identificador único del trace completo
- **Span**: Una unidad de trabajo (un método, una query SQL, un HTTP call)
- **Parent Span**: El span que inició este span
- **Tags**: Metadata (http.method, http.status_code, db.statement)
- **Logs**: Eventos que ocurrieron durante el span

### Información en Spans

Cada span captura:
- **Duration**: Cuánto tiempo tomó
- **Start Time**: Cuándo empezó
- **Tags**: `http.method=GET`, `http.route=/api/orders/{id}`, `db.statement=SELECT * FROM Orders`
- **Logs**: Eventos como excepciones
- **Baggage**: Context propagation entre servicios

### Casos de Uso

**Debugging de Latencia**:
- Identifica cuál span toma más tiempo
- Detecta N+1 queries
- Encuentra llamadas redundantes

**Análisis de Errores**:
- Ve el stack trace completo en contexto
- Identifica en qué servicio/componente falló
- Reproduce el flujo exacto del request fallido

---

## Quick Start

### Opción 1: Stack Completo (Observability + 3 APIs)

```bash
# Levantar Prometheus, Grafana, Jaeger, Node Exporter y las 3 APIs
docker-compose -f docker-compose.observability.yml --profile all up -d

# URLs:
# - Grafana: http://localhost:3000 (admin/admin)
# - Prometheus: http://localhost:9090
# - Jaeger: http://localhost:16686
# - Clean API: http://localhost:5001
# - DDD API: http://localhost:5100
# - EDA API: http://localhost:5200
```

### Opción 2: Solo Observability Stack

```bash
# Solo Prometheus, Grafana, Jaeger, Node Exporter
docker-compose -f docker-compose.observability.yml up -d prometheus grafana jaeger node-exporter

# Luego corre las APIs localmente con dotnet run
cd src/CleanArchitecture/API
dotnet run
```

### Opción 3: APIs Individuales

```bash
# Stack + CleanArchitecture
docker-compose -f docker-compose.observability.yml --profile clean up -d

# Stack + DDD
docker-compose -f docker-compose.observability.yml --profile ddd up -d

# Stack + EDA
docker-compose -f docker-compose.observability.yml --profile eda up -d
```

### Verificar que todo funciona

1. **Prometheus Targets**: http://localhost:9090/targets → Todas las APIs deben estar **UP**
2. **Grafana Dashboards**: http://localhost:3000 → Login → Dashboards
3. **Generar tráfico**:
   ```bash
   # CleanArchitecture
   curl http://localhost:5001/api/customers

   # DDD
   curl http://localhost:5100/api/v1/orders

   # EDA
   curl http://localhost:5200/api/v1/orders
   ```
4. **Ver métricas en Grafana**: Refresh dashboard (debería ver requests)
5. **Ver traces en Jaeger**: http://localhost:16686 → Select service → Find Traces

---

## Métricas Disponibles

### HTTP Metrics

| Métrica | Descripción | Labels |
|---------|-------------|--------|
| `http_server_request_duration_seconds_count` | Total de requests | `method`, `route`, `http_response_status_code`, `service` |
| `http_server_request_duration_seconds_sum` | Suma de duraciones | `method`, `route`, `http_response_status_code`, `service` |
| `http_server_request_duration_seconds_bucket` | Histograma de duraciones | `method`, `route`, `http_response_status_code`, `le`, `service` |
| `http_server_active_requests` | Requests activos en este momento | `method`, `service` |

### .NET Runtime Metrics

| Métrica | Descripción |
|---------|-------------|
| `dotnet_gc_collection_count_total` | Total de GC collections por generación |
| `dotnet_gc_memory_total_available_bytes` | Memoria disponible |
| `dotnet_gc_heap_size_bytes` | Tamaño del heap |
| `dotnet_threadpool_thread_count` | Threads en el threadpool |
| `dotnet_monitor_lock_contention_count_total` | Lock contentions |

### Process Metrics

| Métrica | Descripción |
|---------|-------------|
| `process_cpu_seconds_total` | CPU time total |
| `process_working_set_bytes` | Working set memory |
| `process_private_memory_bytes` | Private memory |
| `process_num_threads` | Número de threads |
| `process_start_time_seconds` | Timestamp de inicio del proceso |

### Database Metrics (SQL)

| Métrica | Descripción |
|---------|-------------|
| `db_client_operation_duration_seconds` | Duración de queries SQL |

---

## Dashboards

### Accediendo a Dashboards

1. **Abrir Grafana**: http://localhost:3000
2. **Login**: admin/admin
3. **Menú lateral**: Dashboards → Browse
4. **Folder**: Software Architecture

### Dashboard 1: ASP.NET Core API Metrics

**Descripción**: Monitoreo completo de las 3 APIs

**Panels**:
- **Request Rate**: Visualiza carga en tiempo real
- **Response Time (P50, P90, P99)**: Latencias percentiles
- **HTTP Status Codes**: Distribución de respuestas
- **Error Rate**: % de errores 5xx
- **Total Requests**: Total de requests en timeframe
- **Active Connections**: Connections concurrentes
- **.NET GC Collections**: Garbage collector activity
- **Memory Usage**: Working set y available memory
- **CPU Usage**: % de CPU por servicio
- **Request Duration Heatmap**: Distribución visual de latencias

**Usos**:
- Detectar spikes de tráfico
- Identificar degradación de performance
- Monitorear error rates
- Detectar memory leaks
- Validar que GC no está siendo agresivo

### Dashboard 2: System Metrics

**Descripción**: Métricas de infraestructura del host

**Panels**:
- **CPU Usage**: % de uso del CPU
- **Memory Usage**: % de memoria usada
- **Disk I/O**: Throughput de lectura/escritura
- **Network I/O**: Bandwidth de red
- **System Load**: Load average (1min)
- **Uptime**: Tiempo desde boot
- **Disk Space**: Uso de disco por filesystem

**Usos**:
- Detectar resource exhaustion
- Planificar capacity
- Detectar anomalías de infraestructura
- Correlacionar problemas de app con sistema

---

## Alerting (Futuro)

Prometheus soporta alerting vía **Alertmanager**. Ejemplo de alerta:

```yaml
# alerts/api_alerts.yml
groups:
  - name: api_alerts
    interval: 30s
    rules:
      - alert: HighErrorRate
        expr: |
          (sum(rate(http_server_request_duration_seconds_count{http_response_status_code=~"5.."}[5m]))
          /
          sum(rate(http_server_request_duration_seconds_count[5m]))) * 100 > 5
        for: 5m
        labels:
          severity: critical
        annotations:
          summary: "High error rate detected"
          description: "Error rate is {{ $value }}% (threshold: 5%)"

      - alert: HighLatency
        expr: |
          histogram_quantile(0.99,
            sum(rate(http_server_request_duration_seconds_bucket[5m])) by (le)
          ) > 1
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High P99 latency detected"
          description: "P99 latency is {{ $value }}s (threshold: 1s)"
```

**Integración con Alertmanager**:
- **Email**: Enviar alertas por email
- **Slack**: Notificaciones en Slack
- **PagerDuty**: On-call rotation
- **Webhook**: Custom integrations

---

## Best Practices

### 1. **Usa Labels Apropiadamente**

 **BUENO**:
```csharp
activity.SetTag("http.method", "GET");
activity.SetTag("http.route", "/api/orders/{id}");
activity.SetTag("order.status", "completed");
```

 **MALO** (alta cardinalidad):
```csharp
activity.SetTag("order.id", orderId);  // Único por request
activity.SetTag("customer.email", email);  // PII + alta cardinalidad
```

**Regla**: Labels deben tener **baja cardinalidad** (pocos valores únicos).

### 2. **Samplea Traces en Producción**

En producción con mucho tráfico, no traces cada request:

```csharp
.WithTracing(tracing => tracing
    .SetSampler(new TraceIdRatioBasedSampler(0.1))  // 10% de traces
)
```

### 3. **Agrega Custom Metrics Cuando Sea Necesario**

```csharp
using var meter = new Meter("MyApp.Orders");
var orderCounter = meter.CreateCounter<long>("orders.created.count");

// En tu código
orderCounter.Add(1, new KeyValuePair<string, object?>("status", "success"));
```

### 4. **Logs, Metrics y Traces Juntos**

**Correlation**: Incluye `TraceId` en logs:

```csharp
Log.Information(
    "Order {OrderId} created. TraceId: {TraceId}",
    orderId,
    Activity.Current?.TraceId
);
```

Luego en Grafana puedes saltar de métrica → log → trace.

### 5. **Dashboards por Audiencia**

- **Developers**: Request latency, error rate, traces
- **SREs**: System metrics, uptime, resource usage
- **Business**: Custom metrics (orders/day, revenue, conversions)

### 6. **Retention Policies**

Métricas ocupan espacio. Configura retention en Prometheus:

```yaml
# prometheus.yml
global:
  retention: 30d  # Retener 30 días de métricas
```

Para largo plazo, usa **remote write** a Thanos/Cortex/Mimir.

---

## Troubleshooting

### Grafana no muestra datos

**Síntomas**: Dashboards vacíos o "No Data"

**Solución**:
1. Verifica que Prometheus esté scraping:
   - http://localhost:9090/targets → Todos deben estar **UP**
2. Verifica datasource en Grafana:
   - Configuration → Data Sources → Prometheus → Test
3. Verifica que las APIs estén exponiendo métricas:
   - http://localhost:5001/metrics (debería ver texto plano de métricas)
4. Revisa logs de Prometheus:
   ```bash
   docker logs prometheus
   ```

### Jaeger no muestra traces

**Síntomas**: "No traces found" en Jaeger UI

**Solución**:
1. Verifica que las APIs tengan configurado el OTLP endpoint:
   ```csharp
   options.Endpoint = new Uri("http://jaeger:4317");
   ```
2. Genera tráfico en las APIs (Jaeger solo muestra traces si hay requests)
3. Revisa logs de Jaeger:
   ```bash
   docker logs jaeger
   ```
4. Verifica que OpenTelemetry esté configurado correctamente (checa logs de la API)

### Prometheus no puede hacer scrape

**Síntomas**: Targets en estado **DOWN** en http://localhost:9090/targets

**Solución**:
1. Verifica conectividad de red:
   ```bash
   docker exec prometheus ping clean-api
   ```
2. Verifica que el endpoint `/metrics` esté accesible:
   ```bash
   docker exec prometheus wget -O- http://clean-api:8080/metrics
   ```
3. Revisa `prometheus.yml`:
   - Target correcto: `clean-api:8080` (no `localhost`)
   - Metrics path: `/metrics`
4. Reinicia Prometheus:
   ```bash
   docker restart prometheus
   ```

### Dashboards no se auto-cargan

**Síntomas**: Dashboards folder está vacío en Grafana

**Solución**:
1. Verifica que los archivos JSON estén en `observability/grafana/dashboards/`
2. Verifica que el volume mount esté correcto en docker-compose:
   ```yaml
   volumes:
     - ./observability/grafana/dashboards:/var/lib/grafana/dashboards:ro
   ```
3. Reinicia Grafana:
   ```bash
   docker restart grafana
   ```
4. Verifica logs:
   ```bash
   docker logs grafana | grep dashboard
   ```

### High memory usage en Prometheus

**Síntomas**: Prometheus consume mucha RAM

**Solución**:
1. Reduce retention:
   ```yaml
   --storage.tsdb.retention.time=15d  # Default: 15 días
   ```
2. Reduce scrape interval:
   ```yaml
   scrape_interval: 30s  # Default: 15s
   ```
3. Usa sampling en traces (no relacionado a Prometheus, pero ayuda a Jaeger)

### No veo métricas de .NET runtime

**Síntomas**: Faltan métricas de GC, memory, etc.

**Solución**:
Verifica que `AddRuntimeInstrumentation()` esté presente:
```csharp
.WithMetrics(metrics => metrics
    .AddRuntimeInstrumentation()  // <-- Esto
    .AddProcessInstrumentation()
)
```

---

## Recursos Adicionales

### Documentación Oficial

- **OpenTelemetry**: https://opentelemetry.io/docs/
- **Prometheus**: https://prometheus.io/docs/
- **Grafana**: https://grafana.com/docs/
- **Jaeger**: https://www.jaegertracing.io/docs/

### PromQL Cheat Sheet

- **Rate**: `rate(metric[5m])` - Incremento por segundo
- **Increase**: `increase(metric[1h])` - Incremento total
- **Sum**: `sum(metric) by (label)` - Agregar por label
- **Histogram Quantile**: `histogram_quantile(0.95, metric)` - P95

### Videos Recomendados

- [Observability with OpenTelemetry](https://www.youtube.com/watch?v=W_8MHdtrgZE)
- [Prometheus Tutorial](https://www.youtube.com/watch?v=h4Sl21AKiDg)

---

## Conclusión

Con este stack de observability tienes:

 **Métricas en tiempo real** con Prometheus
 **Dashboards visuales** con Grafana
 **Distributed tracing** con Jaeger
 **Logs estructurados** con Serilog

Esto te da **visibilidad completa** de tus 3 aplicaciones (CleanArchitecture, DDD, EDA) para:
- Detectar problemas proactivamente
- Debuggear errores rápidamente
- Optimizar performance basándote en datos reales
- Cumplir con SLAs y SLOs

**Next Steps**:
- Configura alertas con Alertmanager
- Agrega custom metrics para business KPIs
- Implementa correlation entre logs y traces
- Configura long-term storage con Thanos/Mimir
