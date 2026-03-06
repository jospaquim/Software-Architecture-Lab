# Resilience Patterns con Polly

## Índice

1. [Introducción](#introducción)
2. [¿Por qué Resilience?](#por-qué-resilience)
3. [Patrones Implementados](#patrones-implementados)
4. [Arquitectura](#arquitectura)
5. [Uso en los Proyectos](#uso-en-los-proyectos)
6. [Best Practices](#best-practices)
7. [Monitoring](#monitoring)
8. [Troubleshooting](#troubleshooting)

---

## Introducción

**Polly** es una librería .NET para resilience y transient-fault-handling. Permite aplicar patrones como Retry, Circuit Breaker, Timeout, Bulkhead y Fallback de manera declarativa.

### Instalación

Los 3 proyectos (CleanArchitecture, DDD, EDA) tienen Polly configurado:

```xml
<PackageReference Include="Polly" Version="8.2.0" />
<PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />
```

---

## ¿Por qué Resilience?

### Sin Resilience

 Un servicio externo tiene un timeout → **Tu API falla**
 Un servicio externo está lento → **Tu API se vuelve lenta**
 Un servicio externo tiene errores transitorios → **Tus usuarios ven errores**
 Un servicio externo cae → **Cascading failure en toda tu arquitectura**

### Con Resilience

 **Retry**: Reintentos automáticos en errores transitorios
 **Circuit Breaker**: Previene cascading failures
 **Timeout**: Limita el tiempo de espera
 **Bulkhead**: Aísla recursos y previene resource exhaustion
 **Fallback**: Respuestas alternativas cuando todo falla

---

## Patrones Implementados

### 1. **Retry Pattern**

**Qué hace**: Reintenta automáticamente requests fallidos

```csharp
// 3 retries con exponential backoff: 2s, 4s, 8s
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (exception, timespan, retryCount, context) =>
        {
            _logger.LogWarning("Retry {RetryCount} after {Delay}s",
                retryCount, timespan.TotalSeconds);
        });
```

**Cuándo usar**:
- Errores de red transitorios
- Rate limiting (429 Too Many Requests)
- Server overload (503 Service Unavailable)
- Database deadlocks

**Cuándo NO usar**:
- Client errors (400, 401, 403, 404) - Son permanentes
- POST no idempotentes - Pueden causar duplicados

---

### 2. **Circuit Breaker Pattern**

**Qué hace**: "Abre" el circuito tras N fallos consecutivos, fail-fast sin intentar llamadas

```
Estado CLOSED (normal) → 5 fallos → Estado OPENED (fail-fast)
    ↑                                        ↓
    ← RESET si OK ← Estado HALF-OPEN (test) ← Después de 30s
```

```csharp
var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5, // 5 fallos
        durationOfBreak: TimeSpan.FromSeconds(30), // 30s abierto
        onBreak: (outcome, duration) =>
        {
            _logger.LogError("Circuit OPENED for {Duration}s", duration.TotalSeconds);
        },
        onReset: () =>
        {
            _logger.LogInformation("Circuit RESET");
        });
```

**Beneficios**:
- Previene cascading failures
- Da tiempo al servicio downstream para recuperarse
- Reduce load en servicios ya sobrecargados
- Falla rápido en lugar de esperar timeouts

---

### 3. **Timeout Pattern**

**Qué hace**: Limita el tiempo máximo de espera

```csharp
var timeoutPolicy = Policy
    .TimeoutAsync<HttpResponseMessage>(
        timeout: TimeSpan.FromSeconds(10),
        timeoutStrategy: TimeoutStrategy.Pessimistic); // Cancela activamente
```

**Tipos**:
- **Pessimistic**: Cancela activamente la operación
- **Optimistic**: Confía en CancellationToken (cooperativo)

---

### 4. **Bulkhead Isolation Pattern**

**Qué hace**: Limita la concurrencia para evitar resource exhaustion

```csharp
var bulkhead = Policy
    .BulkheadAsync<HttpResponseMessage>(
        maxParallelization: 10, // Max 10 concurrent
        maxQueuingActions: 5);   // Max 5 en cola
```

**Beneficio**: Si un servicio se vuelve lento, no consume todos tus threads.

---

### 5. **Fallback Pattern**

**Qué hace**: Proporciona respuesta alternativa si todo falla

```csharp
var fallbackPolicy = Policy<List<Product>>
    .Handle<Exception>()
    .FallbackAsync(
        fallbackValue: new List<Product>(), // Empty list
        onFallbackAsync: async (outcome, context) =>
        {
            _logger.LogError("Fallback activated");
            await Task.CompletedTask;
        });
```

---

## Arquitectura

### Integración con HttpClient

```csharp
// Program.cs - CleanArchitecture
builder.Services.AddHttpClient<IExternalApiClient, ExternalApiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddPolicyHandler((services, request) =>
{
    var logger = services.GetRequiredService<ILogger<Program>>();
    return ResiliencePolicies.GetCombinedPolicy(logger);
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5));
```

### Orden de Ejecución de Políticas

```csharp
// Execution order: Outer → Inner
var combinedPolicy = Policy.WrapAsync(
    circuitBreaker,  // 1. Check if circuit is open
    retry,           // 2. Retry on failures
    timeout);        // 3. Each retry has timeout

// Request flow:
// Circuit Breaker → Retry (attempt 1) → Timeout → HTTP Call
//                → Retry (attempt 2) → Timeout → HTTP Call
//                → Retry (attempt 3) → Timeout → HTTP Call
```

**Regla**: Las políticas se ejecutan de afuera hacia adentro.

---

## Uso en los Proyectos

### CleanArchitecture

**Archivo**: `src/CleanArchitecture/Infrastructure/Resilience/ResiliencePolicies.cs`

**Cliente**: `ExternalApiClient` - Llama APIs externas genéricas

```csharp
public async Task<WeatherForecast?> GetWeatherAsync(string city)
{
    // Polly policies are automatically applied
    var response = await _httpClient.GetAsync($"/weather?q={city}");
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<WeatherForecast>();
}
```

### DDD Sales

**Archivo**: `src/DDD/Sales/Infrastructure/Resilience/ResiliencePolicies.cs`

**Cliente**: `PaymentGatewayClient` - Integración con payment gateways

```csharp
public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
{
    // Circuit breaker prevents cascading failures to payment gateway
    var response = await _httpClient.PostAsJsonAsync("/api/payments", request);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<PaymentResult>();
}
```

### EDA

**Archivo**: `src/EDA/Infrastructure/Resilience/ResiliencePolicies.cs`

**Cliente**: `NotificationServiceClient` - Envío de emails/SMS

```csharp
public async Task SendEmailAsync(EmailNotification notification)
{
    // Retry with exponential backoff for transient email sending failures
    var response = await _httpClient.PostAsJsonAsync("/api/emails", notification);
    response.EnsureSuccessStatusCode();
}
```

---

## Best Practices

### 1. **Usa Exponential Backoff con Jitter**

```csharp
var random = new Random();

sleepDurationProvider: (retryAttempt, context) =>
{
    var exponentialDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
    var jitter = TimeSpan.FromMilliseconds(random.Next(0, 1000));
    return exponentialDelay + jitter; // Evita thundering herd
}
```

**Por qué**: Sin jitter, múltiples clientes reintentan al mismo tiempo, causando spikes.

---

### 2. **No Reintentar Todo**

 **SÍ reintentar**:
- `HttpStatusCode.RequestTimeout` (408)
- `HttpStatusCode.TooManyRequests` (429)
- `HttpStatusCode.InternalServerError` (500)
- `HttpStatusCode.BadGateway` (502)
- `HttpStatusCode.ServiceUnavailable` (503)
- `HttpStatusCode.GatewayTimeout` (504)
- `HttpRequestException` (network errors)
- `TimeoutException`

 **NO reintentar**:
- `BadRequest` (400) - Validation error
- `Unauthorized` (401) - Auth error
- `Forbidden` (403) - Permission error
- `NotFound` (404) - Resource doesn't exist
- `Conflict` (409) - Business rule violation

---

### 3. **Respeta Retry-After Header**

```csharp
.WaitAndRetryAsync(3, (retryAttempt, context) =>
{
    var response = context["LastResponse"] as HttpResponseMessage;
    if (response?.Headers.RetryAfter != null)
    {
        return response.Headers.RetryAfter.Delta ?? TimeSpan.FromSeconds(5);
    }
    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
});
```

---

### 4. **Circuit Breaker Thresholds**

| Tráfico | Failures Before Break | Duration |
|---------|----------------------|----------|
| Bajo (<100 req/min) | 3-5 | 30s |
| Medio (100-1000 req/min) | 5-10 | 30-60s |
| Alto (>1000 req/min) | 10-20 | 60s |

---

### 5. **HttpClient Lifetime**

```csharp
.SetHandlerLifetime(TimeSpan.FromMinutes(5))
```

**Por qué**: `HttpClient` reutiliza sockets. Si el DNS cambia, necesitas recrear el handler.

---

### 6. **Logs Detallados**

```csharp
onRetry: (outcome, timespan, retryCount, context) =>
{
    _logger.LogWarning(
        "Retry {RetryCount}/{MaxRetries} after {Delay}s. " +
        "Reason: {Reason}. " +
        "URL: {Url}. " +
        "Context: {Context}",
        retryCount,
        3,
        timespan.TotalSeconds,
        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString(),
        context.GetValueOrDefault("Url"),
        context.OperationKey);
}
```

---

### 7. **Idempotencia para Retries**

```csharp
//  SEGURO: GET es idempotente
var retryPolicy = Policy.Handle<HttpRequestException>().RetryAsync(3);
await retryPolicy.ExecuteAsync(() => httpClient.GetAsync("/api/data"));

//  PELIGROSO: POST puede crear duplicados
// await retryPolicy.ExecuteAsync(() => httpClient.PostAsync("/api/orders", order));

//  SEGURO: POST con idempotency key
request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
await retryPolicy.ExecuteAsync(() => httpClient.PostAsync("/api/orders", order));
```

---

## Monitoring

### Métricas a Trackear

```csharp
// OpenTelemetry custom metrics
using var meter = new Meter("MyApp.Resilience");

var retryCounter = meter.CreateCounter<long>("polly.retry.count");
var circuitBreakerStateGauge = meter.CreateObservableGauge<int>("polly.circuit_breaker.state");
var timeoutCounter = meter.CreateCounter<long>("polly.timeout.count");

onRetry: (outcome, timespan, retryCount, context) =>
{
    retryCounter.Add(1, new KeyValuePair<string, object?>("policy_name", "http_retry"));
}
```

### Dashboards en Grafana

**Panel**: Retry Rate
```promql
rate(polly_retry_count_total[5m])
```

**Panel**: Circuit Breaker State
```promql
polly_circuit_breaker_state{job="my-api"}
# 0 = Closed, 1 = Open, 2 = Half-Open
```

**Alert**: Circuit Breaker Opened
```yaml
alert: CircuitBreakerOpened
expr: polly_circuit_breaker_state == 1
for: 1m
annotations:
  summary: "Circuit breaker is OPEN for {{ $labels.service }}"
```

---

## Troubleshooting

### "BrokenCircuitException: The circuit is now open"

**Causa**: El circuit breaker se abrió tras múltiples fallos.

**Solución**:
1. Revisa logs para ver qué causó los fallos iniciales
2. Verifica que el servicio downstream esté operativo
3. El circuit breaker se auto-resetea tras el `durationOfBreak`

```csharp
catch (BrokenCircuitException ex)
{
    _logger.LogError("Circuit is OPEN. Service {Service} is unavailable", serviceName);
    // Return fallback response or throw user-friendly error
    throw new ServiceUnavailableException("External service temporarily unavailable");
}
```

---

### "Too many retries, request still failing"

**Causa**: El error no es transitorio, es permanente.

**Solución**:
- No reintentar errores 4xx (client errors)
- Revisar si el error es permanente (404, 403, 400)
- Agregar logging para identificar el error real

```csharp
.Handle<HttpRequestException>()
.OrResult<HttpResponseMessage>(r =>
    r.StatusCode >= HttpStatusCode.InternalServerError) // Solo 5xx
.RetryAsync(3);
```

---

### "Timeouts ocurren incluso con timeout largo"

**Causa**: El servicio downstream realmente es lento.

**Soluciones**:
1. **Optimiza el servicio downstream** (cache, database indexes)
2. **Usa Bulkhead** para limitar concurrencia
3. **Implementa pagination** si estás trayendo mucha data
4. **Usa async processing** (queue + background job) para operaciones lentas

---

### "Sockets exhaustion / Port exhaustion"

**Causa**: HttpClient se crea y destruye constantemente.

**Solución**: Usa `IHttpClientFactory` (ya implementado):

```csharp
//  CORRECTO
builder.Services.AddHttpClient<IMyClient, MyClient>()
    .SetHandlerLifetime(TimeSpan.FromMinutes(5));

//  INCORRECTO
using var httpClient = new HttpClient(); // NO HAGAS ESTO
```

---

## Testing

### Unit Test para Retry Policy

```csharp
[Fact]
public async Task RetryPolicy_ShouldRetry3Times()
{
    // Arrange
    var attemptCount = 0;
    var policy = Policy
        .Handle<Exception>()
        .RetryAsync(3);

    // Act & Assert
    await Assert.ThrowsAsync<Exception>(async () =>
    {
        await policy.ExecuteAsync(() =>
        {
            attemptCount++;
            throw new Exception("Test exception");
        });
    });

    Assert.Equal(4, attemptCount); // 1 original + 3 retries
}
```

### Integration Test con WireMock

```csharp
[Fact]
public async Task HttpClient_WithPolly_ShouldRetryOn503()
{
    // Arrange: Mock server que retorna 503, luego 200
    var server = WireMockServer.Start();
    server
        .Given(Request.Create().WithPath("/api/data").UsingGet())
        .InScenario("Retry")
        .WillSetStateTo("Attempt1")
        .RespondWith(Response.Create().WithStatusCode(503));

    server
        .Given(Request.Create().WithPath("/api/data").UsingGet())
        .InScenario("Retry")
        .WhenStateIs("Attempt1")
        .RespondWith(Response.Create().WithStatusCode(200));

    // Act
    var response = await _httpClient.GetAsync($"{server.Urls[0]}/api/data");

    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal(2, server.LogEntries.Count()); // 1 fail + 1 success
}
```

---

## Recursos

- **Polly Documentation**: https://www.pollydocs.org/
- **Ejemplos detallados**: [POLLY-EXAMPLES.md](POLLY-EXAMPLES.md)
- **Azure Retry Guidance**: https://learn.microsoft.com/azure/architecture/best-practices/retry-service-specific
- **Polly GitHub**: https://github.com/App-vNext/Polly

---

## Conclusión

Con Polly implementado en los 3 proyectos, tienes:

 **Retry automático** en errores transitorios
 **Circuit Breaker** para prevenir cascading failures
 **Timeout** para evitar requests eternos
 **Integración con HttpClient** vía IHttpClientFactory
 **Logging completo** de todos los eventos de resilience

Esto hace que tus aplicaciones sean **resilientes**, **fault-tolerant** y **production-ready**.
