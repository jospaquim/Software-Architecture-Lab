# Observability Guide

Observability is crucial for understanding the internal state of these microservices architectures. 

## Logging
- **Serilog**: Configured across all projects for structured logging.
- **Console and File Sinks**: Configured differently per environment (Development vs Production).

## Metrics
- **Prometheus**: Used for scraping and storing metrics.
- **Grafana**: Recommended for visualization of metrics and dashboards.

## Tracing
- **OpenTelemetry**: Integrated for distributed tracing, allowing developers to track requests across multiple microservice boundaries.
