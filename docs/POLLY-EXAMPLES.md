# Polly Resilience Patterns

Polly is a .NET resilience and transient-fault-handling library. This project utilizes several of its policies.

## Key Policies Implemented
1. **Retry Policy**: Retries an operation multiple times before failing.
2. **Circuit Breaker Policy**: Stops execution of a failing operation temporarily to prevent cascading failures.
3. **Timeout Policy**: Ensures a call doesn't wait forever.
4. **Fallback Policy**: Provides a substitute value or behavior upon failure.
