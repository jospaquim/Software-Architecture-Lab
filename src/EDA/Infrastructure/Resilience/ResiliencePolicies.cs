using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Microsoft.Extensions.Logging;

namespace EDA.Infrastructure.Resilience;

/// <summary>
/// Polly resilience policies for EDA (Event-Driven Architecture)
/// </summary>
public static class ResiliencePolicies
{
    public static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger)
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<TimeoutException>()
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    logger.LogWarning(
                        "[EDA] Retry {RetryCount} after {Delay}s. Reason: {Reason}",
                        retryCount,
                        timespan.TotalSeconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    public static AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(ILogger logger)
    {
        return Policy
            .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
            .Or<TimeoutException>()
            .Or<HttpRequestException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    logger.LogError("[EDA] Circuit Breaker OPENED for {Duration}s", duration.TotalSeconds);
                },
                onReset: () =>
                {
                    logger.LogInformation("[EDA] Circuit Breaker RESET");
                },
                onHalfOpen: () =>
                {
                    logger.LogWarning("[EDA] Circuit Breaker HALF-OPEN");
                });
    }

    public static AsyncTimeoutPolicy<HttpResponseMessage> GetTimeoutPolicy(ILogger logger)
    {
        return Policy
            .TimeoutAsync<HttpResponseMessage>(
                timeout: TimeSpan.FromSeconds(10),
                timeoutStrategy: TimeoutStrategy.Pessimistic,
                onTimeoutAsync: (context, timespan, task) =>
                {
                    logger.LogWarning("[EDA] Request TIMEOUT after {Timeout}s", timespan.TotalSeconds);
                    return Task.CompletedTask;
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> GetCombinedPolicy(ILogger logger)
    {
        var timeout = GetTimeoutPolicy(logger);
        var retry = GetRetryPolicy(logger);
        var circuitBreaker = GetCircuitBreakerPolicy(logger);

        return Policy.WrapAsync(circuitBreaker, retry, timeout);
    }
}
