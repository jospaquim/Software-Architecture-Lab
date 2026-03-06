# Resilience In System Design

Resilience strategies ensure the system remains responsive even under significant load or when external dependencies fail.

## Strategies Utilized
- **Idempotency**: Guaranteeing that retried requests do not result in unintended side effects.
- **Bulkheading**: Isolating resources to prevent a failure in one area from affecting others.
- **Health Checks**: Continuous monitoring of dependencies allowing for automated failover.
