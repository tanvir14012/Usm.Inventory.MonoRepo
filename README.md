# Usm.Inventory.MonoRepo

Usm.Inventory.MonoRepo is a .NET-based US military inventory management platform. It is organized as a monorepo of domain-focused services that handle identity, administration, warehousing, issue and receipt flows, procurement, maintenance, reporting, and related support capabilities.

## Project structure

- `Services/` - business services grouped by bounded context, such as `Identity`, `StoreHouse`, `IssueReceipt`, `Procurement`, and `Reporting`
- `Services/*/src/*Api` - HTTP API entry points
- `Services/*/src/*Application` - application use cases, MediatR handlers, and validation wiring
- `Services/*/src/*Domain` - core domain models and business rules
- `Services/*/src/*Infrastructure` - persistence, integration, messaging, and external system implementations
- `Shared/` - reusable building blocks, contracts, caching, EF extensions, messaging, localization, validation, and utilities
- `Gateway/ApiGateway` - reverse proxy and single entry point to backend APIs
- `AppHost/` - .NET Aspire-style local orchestration for infrastructure and service startup
- `ServiceDefaults/` - shared defaults for telemetry, health checks, service discovery, and cross-cutting host configuration

## Design patterns

- **Clean Architecture** - each service is split into API, Application, Domain, and Infrastructure layers to keep business rules isolated from delivery and persistence concerns
- **CQRS with MediatR** - requests are modeled as commands and queries with handlers in the application layer
- **Bounded Contexts** - each service owns a focused domain area and evolves independently within the monorepo
- **Dependency Injection and shared bootstrap** - common concerns such as authentication, logging, configuration, health checks, and observability are applied through shared extensions
- **Pipeline validation** - FluentValidation is applied through MediatR pipeline behaviors before handlers execute

## Notes

The repository combines service autonomy with shared platform components so teams can build inventory capabilities consistently while reusing common infrastructure and operational patterns.

## Telemetry, audit logs, and observability

- Services emit traces and metrics through OpenTelemetry (OTLP), including ASP.NET Core, outgoing HTTP calls, and runtime metrics.
- Structured application and audit logs are captured through Serilog and can be shipped to Loki.
- `docker-compose.yml` now runs a GLP observability stack plus OTEL collector:
  - Grafana (`:3000`)
  - Loki (`:3100`)
  - Prometheus (`:9090`)
  - OpenTelemetry Collector (`:4317`, `:4318`, `:8889`)
  - Jaeger UI (`:16686`)

## Azure DevOps CI/CD and Kubernetes workflow

- `azure-pipelines.yml` includes:
  - **BuildAndTest**: restore, build, test solution
  - **BuildAndPushImages**: build/push backend and gateway images to ACR on `master`
  - **DeployToAks**: apply Kubernetes manifests with image tag injection
- Kubernetes manifests are in `Platform/Kubernetes/`:
  - `namespace.yaml`
  - `observability.yaml` (GLP + OTEL collector stack)
  - `workloads.yaml` (identity API and gateway workloads)