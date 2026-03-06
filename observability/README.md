# Observability Stack

This directory contains the configuration for the observability stack:
- **Prometheus**: Metrics collection and storage
- **Grafana**: Metrics visualization and dashboards
- **Jaeger**: Distributed tracing

## Directory Structure

```
observability/
├── prometheus/
│   └── prometheus.yml          # Prometheus configuration
├── grafana/
│   ├── provisioning/
│   │   ├── datasources/        # Auto-configured datasources
│   │   │   └── datasources.yml
│   │   └── dashboards/         # Dashboard provisioning
│   │       └── dashboards.yml
│   └── dashboards/             # Grafana dashboards (JSON)
│       ├── aspnet-api-metrics.json
│       └── system-metrics.json
└── README.md
```

## Quick Start

```bash
# Start the observability stack
docker-compose -f docker-compose.observability.yml up -d prometheus grafana jaeger node-exporter

# Start all 3 APIs with observability
docker-compose -f docker-compose.observability.yml --profile all up -d

# Or start individual APIs
docker-compose -f docker-compose.observability.yml --profile clean up -d
docker-compose -f docker-compose.observability.yml --profile ddd up -d
docker-compose -f docker-compose.observability.yml --profile eda up -d
```

## Access URLs

- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090
- **Jaeger UI**: http://localhost:16686
- **Node Exporter**: http://localhost:9100/metrics

## Metrics Endpoints

Each API exposes metrics at `/metrics`:
- CleanArchitecture API: http://localhost:5001/metrics
- DDD Sales API: http://localhost:5100/metrics
- EDA API: http://localhost:5200/metrics

## Dashboards

Grafana comes pre-configured with dashboards:
1. **ASP.NET Core API Metrics**: Comprehensive API monitoring
2. **System Metrics**: Infrastructure monitoring

Dashboards are automatically loaded on startup.

## Adding Custom Dashboards

1. Create dashboard in Grafana UI
2. Export as JSON
3. Place in `observability/grafana/dashboards/`
4. Restart Grafana to load

## Troubleshooting

**Grafana doesn't show data**:
- Check Prometheus datasource in Grafana (Configuration > Data Sources)
- Verify APIs are running and exposing `/metrics`
- Check Prometheus targets: http://localhost:9090/targets

**Jaeger doesn't show traces**:
- Verify `OpenTelemetry:OtlpEndpoint` is set to `http://jaeger:4317` in API configs
- Check Jaeger health: http://localhost:14269

**Prometheus can't scrape metrics**:
- Verify network connectivity between containers
- Check `prometheus.yml` target configuration
- Inspect Prometheus logs: `docker logs prometheus`
