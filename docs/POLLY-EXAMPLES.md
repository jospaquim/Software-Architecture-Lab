# Polly Resilience Patterns - Usage Examples

## Table of Contents

1. [Basic Retry](#basic-retry)
2. [Circuit Breaker](#circuit-breaker)
3. [Timeout](#timeout)
4. [Combined Policies](#combined-policies)
5. [HttpClient Integration](#httpclient-integration)
6. [Custom Scenarios](#custom-scenarios)

---

## Basic Retry

### Simple Retry (3 attempts)

```csharp
using Polly;

var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .RetryAsync(3);

await retryPolicy.ExecuteAsync(async () =>
{
    var response = await httpClient.GetAsync("https://api.example.com/data");
    response.EnsureSuccessStatusCode();
});
```

### Retry with Exponential Backoff

```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2s, 4s, 8s
        onRetry: (exception, timespan, retryCount, context) =>
        {
            _logger.LogWarning(
                "Retry {RetryCount} after {Delay}s due to {Exception}",
                retryCount,
                timespan.TotalSeconds,
                exception.Message);
        });
```

### Retry with Jitter (Prevent Thundering Herd)

```csharp
var random = new Random();

var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: (retryAttempt, context) =>
        {
            var exponentialDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
            var jitter = TimeSpan.FromMilliseconds(random.Next(0, 1000));
            return exponentialDelay + jitter; // 2-3s, 4-5s, 8-9s
        });
```

---

## Circuit Breaker

### Basic Circuit Breaker

```csharp
var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5, // Open after 5 failures
        durationOfBreak: TimeSpan.FromSeconds(30), // Stay open for 30s
        onBreak: (exception, duration) =>
        {
            _logger.LogError("Circuit OPENED for {Duration}s", duration.TotalSeconds);
        },
        onReset: () =>
        {
            _logger.LogInformation("Circuit RESET - Back to normal");
        },
        onHalfOpen: () =>
        {
            _logger.LogWarning("Circuit HALF-OPEN - Testing recovery");
        });
```

### Circuit Breaker States

```
┌────────────┐
│   CLOSED   │  ◄── Normal operation
└─────┬──────┘
      │ 5 failures
      ▼
┌────────────┐
│   OPENED   │  ◄── All requests fail-fast
└─────┬──────┘
      │ After 30s
      ▼
┌────────────┐
│ HALF-OPEN  │  ◄── Allow 1 test request
└─────┬──────┘
      │
      ├──► Success: Back to CLOSED
      └──► Failure: Back to OPENED
```

---

## Timeout

### Pessimistic Timeout (Cancels the operation)

```csharp
var timeoutPolicy = Policy
    .TimeoutAsync<HttpResponseMessage>(
        timeout: TimeSpan.FromSeconds(10),
        timeoutStrategy: TimeoutStrategy.Pessimistic, // Actively cancels
        onTimeoutAsync: (context, timespan, task) =>
        {
            _logger.LogWarning("Request timed out after {Timeout}s", timespan.TotalSeconds);
            return Task.CompletedTask;
        });

await timeoutPolicy.ExecuteAsync(async ct =>
{
    return await httpClient.GetAsync("https://slow-api.com/data", ct);
});
```

### Optimistic Timeout (Cooperative cancellation)

```csharp
var timeoutPolicy = Policy
    .TimeoutAsync<HttpResponseMessage>(
        timeout: TimeSpan.FromSeconds(5),
        timeoutStrategy: TimeoutStrategy.Optimistic); // Relies on CancellationToken

// The HttpClient call must respect the CancellationToken for this to work
await timeoutPolicy.ExecuteAsync(async ct =>
{
    return await httpClient.GetAsync("https://api.com/data", ct);
});
```

---

## Combined Policies

### Wrap Multiple Policies (Order Matters!)

```csharp
var timeout = Policy
    .TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10), TimeoutStrategy.Pessimistic);

var retry = Policy
    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .Or<TimeoutException>()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

var circuitBreaker = Policy
    .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
    .Or<TimeoutException>()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

// Execution order: Circuit Breaker → Retry → Timeout
// Outer policies wrap inner policies
var combinedPolicy = Policy.WrapAsync(circuitBreaker, retry, timeout);

await combinedPolicy.ExecuteAsync(async () =>
{
    return await httpClient.GetAsync("https://api.example.com/data");
});
```

**Execution Flow**:
1. **Circuit Breaker** checks if circuit is open
2. If closed, **Retry** wraps the call
3. Each retry attempt has a **Timeout**

---

## HttpClient Integration

### Typed HttpClient with Polly

```csharp
// In Program.cs
services.AddHttpClient<IExternalApiClient, ExternalApiClient>(client =>
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

// Usage in a service
public class MyService
{
    private readonly IExternalApiClient _apiClient;

    public MyService(IExternalApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task DoWorkAsync()
    {
        // Polly policies are automatically applied
        var data = await _apiClient.GetDataAsync();
    }
}
```

### Named HttpClient with Different Policies for GET/POST

```csharp
services.AddHttpClient("ResilientClient")
    // GET requests: Retry with jitter
    .AddPolicyHandler((services, request) =>
    {
        return request.Method == HttpMethod.Get
            ? RetryPolicyWithJitter()
            : Policy.NoOpAsync<HttpResponseMessage>();
    })
    // All requests: Circuit Breaker
    .AddPolicyHandler(CircuitBreakerPolicy())
    // All requests: Timeout
    .AddPolicyHandler(TimeoutPolicy());

// Usage
var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();
var client = httpClientFactory.CreateClient("ResilientClient");
var response = await client.GetAsync("https://api.example.com/data");
```

---

## Custom Scenarios

### Scenario 1: Database Retry with Transient Error Detection

```csharp
var dbRetryPolicy = Policy
    .Handle<SqlException>(ex =>
        ex.Number == -2 ||       // Timeout
        ex.Number == 1205 ||     // Deadlock
        ex.Number == 40197 ||    // Azure: Service unavailable
        ex.Number == 40501)      // Azure: Busy
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (exception, timespan, retryCount, context) =>
        {
            _logger.LogWarning(
                "Database operation retry {RetryCount} after {Delay}s. SQL Error: {SqlError}",
                retryCount,
                timespan.TotalSeconds,
                ((SqlException)exception).Number);
        });

await dbRetryPolicy.ExecuteAsync(async () =>
{
    await dbContext.SaveChangesAsync();
});
```

### Scenario 2: Fallback Policy (Return Default Value on Failure)

```csharp
var fallbackPolicy = Policy<List<Product>>
    .Handle<Exception>()
    .Or<BrokenCircuitException>()
    .FallbackAsync(
        fallbackValue: new List<Product>(), // Return empty list
        onFallbackAsync: async (outcome, context) =>
        {
            _logger.LogError("Fallback activated. Returning empty product list.");
            await Task.CompletedTask;
        });

var products = await fallbackPolicy.ExecuteAsync(async () =>
{
    return await _apiClient.GetProductsAsync();
});

// If API fails, returns empty list instead of throwing
```

### Scenario 3: Bulkhead Isolation (Limit Concurrency)

```csharp
var bulkhead = Policy
    .BulkheadAsync<HttpResponseMessage>(
        maxParallelization: 10, // Max 10 concurrent calls
        maxQueuingActions: 5,   // Max 5 queued
        onBulkheadRejectedAsync: async context =>
        {
            _logger.LogWarning("Bulkhead rejected request - too many concurrent calls");
            await Task.CompletedTask;
        });

await bulkhead.ExecuteAsync(async () =>
{
    return await httpClient.GetAsync("https://api.example.com/data");
});
```

### Scenario 4: Conditional Retry (Retry only for specific status codes)

```csharp
var conditionalRetryPolicy = Policy
    .HandleResult<HttpResponseMessage>(r =>
        r.StatusCode == HttpStatusCode.RequestTimeout ||
        r.StatusCode == HttpStatusCode.TooManyRequests ||
        r.StatusCode == HttpStatusCode.ServiceUnavailable)
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt =>
        {
            // For 429 (Too Many Requests), use Retry-After header if present
            if (lastResponse?.StatusCode == HttpStatusCode.TooManyRequests &&
                lastResponse.Headers.RetryAfter != null)
            {
                return lastResponse.Headers.RetryAfter.Delta ?? TimeSpan.FromSeconds(5);
            }
            return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
        });
```

### Scenario 5: Context-Aware Policies (Pass metadata)

```csharp
var policy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(
        retryCount: 3,
        sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (exception, timespan, retryCount, context) =>
        {
            var userId = context.GetValueOrDefault("UserId");
            var operation = context.GetValueOrDefault("Operation");

            _logger.LogWarning(
                "User {UserId} - Operation {Operation} - Retry {RetryCount}",
                userId,
                operation,
                retryCount);
        });

// Pass context
var context = new Context
{
    ["UserId"] = currentUserId,
    ["Operation"] = "GetOrders"
};

await policy.ExecuteAsync(async ctx =>
{
    return await httpClient.GetAsync("https://api.example.com/orders");
}, context);
```

---

## Best Practices

### 1. **Always Log Policy Events**
```csharp
onRetry: (outcome, timespan, retryCount, context) =>
{
    _logger.LogWarning("Retry {Count} after {Delay}s", retryCount, timespan.TotalSeconds);
}
```

### 2. **Use Exponential Backoff with Jitter**
Prevents thundering herd problem when many clients retry simultaneously.

### 3. **Set Appropriate Timeouts**
- Short timeout for fast-fail scenarios (1-5s)
- Longer timeout for heavy operations (30-60s)

### 4. **Circuit Breaker Thresholds**
- **Low traffic**: 3-5 failures
- **High traffic**: 10-20 failures
- **Duration of break**: 30-60 seconds

### 5. **HttpClient Lifetime**
```csharp
.SetHandlerLifetime(TimeSpan.FromMinutes(5))
```
Prevents socket exhaustion by recycling HttpClientHandler.

### 6. **Don't Retry Everything**
-  Retry: Transient errors (timeout, 500, 503)
-  Don't retry: Client errors (400, 401, 403, 404)

### 7. **Monitor Circuit Breaker State**
Expose metrics to Prometheus/Grafana to track when circuits open.

---

## Monitoring Polly with OpenTelemetry

### Add custom metrics for policies

```csharp
using var meter = new Meter("MyApp.Resilience");
var retryCounter = meter.CreateCounter<long>("polly.retry.count");
var circuitBreakerState = meter.CreateObservableGauge<int>("polly.circuit_breaker.state");

var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (exception, timespan, retryCount, context) =>
        {
            retryCounter.Add(1, new KeyValuePair<string, object?>("policy", "http-retry"));
            _logger.LogWarning("Retry {Count}", retryCount);
        });
```

---

## Testing Polly Policies

### Unit Test for Retry Policy

```csharp
[Fact]
public async Task RetryPolicy_ShouldRetry3Times_OnTransientFailure()
{
    // Arrange
    var attemptCount = 0;
    var policy = ResiliencePolicies.GetRetryPolicy(_logger);

    // Act
    try
    {
        await policy.ExecuteAsync(async () =>
        {
            attemptCount++;
            if (attemptCount < 4)
            {
                throw new HttpRequestException("Transient error");
            }
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
    }
    catch
    {
        // Expected
    }

    // Assert
    Assert.Equal(4, attemptCount); // Initial + 3 retries
}
```

---

## Resources

- **Polly Documentation**: https://www.pollydocs.org/
- **Polly GitHub**: https://github.com/App-vNext/Polly
- **Retry Guidance**: https://learn.microsoft.com/en-us/azure/architecture/best-practices/retry-service-specific
