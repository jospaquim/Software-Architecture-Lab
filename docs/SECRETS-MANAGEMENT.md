# Secrets Management

Handling sensitive information securely within the boilerplates.

## Approaches
- **Development Environment**: Utilization of `appsettings.Development.json` and .NET user secrets.
- **Docker Environment**: Reading secrets from `.env` files and Docker Secrets.
- **Kubernetes**: Managing credentials via Kubernetes Secrets objects and Azure Key Vault integration.
