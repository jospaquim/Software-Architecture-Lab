using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Microsoft.Extensions.Logging;
using System.Net;

namespace CleanArchitecture.Infrastructure.Resilience;

/// <summary>
/// Polly resilience policies for CleanArchitecture
/// Provides Circuit Breaker, Retry, Timeout and Bulkhead patterns
/// </summary>
public static class ResiliencePolicies
{
    /// <summary>
    /// Retry policy: 3 retries with exponential backoff
    /// </summary>
    public static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r =>
                !r.IsSuccessStatusCode ||
                r.StatusCode == HttpStatusCode.RequestTimeout ||
                r.StatusCode == HttpStatusCode.TooManyRequests)
            .Or<TimeoutException>()
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2s, 4s, 8s
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    logger.LogWarning(
                        "Retry {RetryCount} after {Delay}s due to {Reason}. Context: {Context}",
                        retryCount,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString(),
                        context.OperationKey);
                });
    }

    /// <summary>
    /// Circuit Breaker policy: Opens after 5 consecutive failures, half-opens after 30s
    /// </summary>
    public static AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger logger)
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<TimeoutException>()
            .Or<HttpRequestException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5, // Open after 5 failures
                durationOfBreak: TimeSpan.FromSeconds(30), // Half-open after 30s
                onBreak: (outcome, duration) =>
                {
                    logger.LogError(
                        "Circuit Breaker OPENED for {Duration}s due to {Reason}",
                        duration.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit Breaker RESET - Back to normal operation");
                },
                onHalfOpen: () =>
                {
                    logger.LogWarning("Circuit Breaker HALF-OPEN - Testing if service recovered");
                });
    }

    /// <summary>
    /// Timeout policy: 10 seconds timeout
    /// </summary>
    public static AsyncTimeoutPolicy<HttpResponseMessage> GetTimeoutPolicy(ILogger logger)
    {
        return Policy
            .TimeoutAsync<HttpResponseMessage>(
                timeout: TimeSpan.FromSeconds(10),
                timeoutStrategy: TimeoutStrategy.Pessimistic,
                onTimeoutAsync: (context, timespan, task) =>
                {
                    logger.LogWarning(
                        "Request TIMEOUT after {Timeout}s. Context: {Context}",
                        timespan.TotalSeconds,
                        context.OperationKey);
                    return Task.CompletedTask;
                });
    }

    /// <summary>
    /// Combined resilience policy: Timeout → Retry → Circuit Breaker
    /// Order matters! Inner policies execute first.
    /// </summary>
    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(ILogger logger)
    {
        var timeout = GetTimeoutPolicy(logger);
        var retry = GetRetryPolicy(logger);
        var circuitBreaker = GetCircuitBreakerPolicy(logger);

        // Execution order: Timeout → Retry → Circuit Breaker
        return Policy.WrapAsync(circuitBreaker, retry, timeout);
    }

    /// <summary>
    /// Advanced retry policy with jitter to prevent thundering herd
    /// </summary>
    public static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicyWithJitter(ILogger logger)
    {
        var random = new Random();

        return Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<TimeoutException>()
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: (retryAttempt, context) =>
                {
                    // Exponential backoff with jitter
                    var exponentialDelay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    var jitter = TimeSpan.FromMilliseconds(random.Next(0, 1000));
                    return exponentialDelay + jitter;
                },
                onRetryAsync: async (outcome, timespan, retryCount, context) =>
                {
                    logger.LogWarning(
                        "Retry {RetryCount} after {Delay}ms with jitter. Reason: {Reason}",
                        retryCount,
                        timespan.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    await Task.CompletedTask;
                });
    }

    /// <summary>
    /// Optimistic timeout for fast-fail scenarios
    /// </summary>
    public static AsyncTimeoutPolicy<HttpResponseMessage> GetOptimisticTimeoutPolicy()
    {
        return Policy
            .TimeoutAsync<HttpResponseMessage>(
                timeout: TimeSpan.FromSeconds(5),
                timeoutStrategy: TimeoutStrategy.Optimistic); // Cooperative cancellation
    }

    /// <summary>
    /// Fallback policy: Returns default response on failure
    /// </summary>
    public static AsyncPolicy<HttpResponseMessage> GetFallbackPolicy(ILogger logger)
    {
        return Policy<HttpResponseMessage>
            .Handle<Exception>()
            .Or<TimeoutException>()
            .Or<BrokenCircuitException>()
            .FallbackAsync(
                fallbackValue: new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    Content = new StringContent("{\"error\": \"Service temporarily unavailable. Please try again later.\"}")
                },
                onFallbackAsync: async (outcome, context) =>
                {
                    logger.LogError(
                        "FALLBACK activated due to {Reason}. Returning default response.",
                        outcome.Exception?.Message ?? "Unknown error");
                    await Task.CompletedTask;
                });
    }
}
