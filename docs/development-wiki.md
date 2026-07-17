# Development Wiki

This page is the technical handbook for contributors working in `Usm.Inventory.MonoRepo`. It is intended to support day-to-day development and long-term maintenance.

## 1. Repository overview

The repository is a monorepo for a military inventory platform, built around domain-aligned backend services plus a single Angular frontend.

### Primary top-level directories

| Path | Purpose |
| --- | --- |
| `Services/` | Domain services (Identity, Iam, Administration, StoreHouse, IssueReceipt, Procurement, Reporting, etc.) |
| `Shared/` | Reusable cross-service components (contracts, validation, messaging, caching, utilities) |
| `Gateway/ApiGateway` | Reverse proxy/API entrypoint (YARP) |
| `Frontend/Angular` | Angular SPA |
| `AppHost/` | Local orchestration host for development |
| `ServiceDefaults/` | Shared service bootstrapping defaults (telemetry, health checks, host conventions) |
| `Platform/` | Infrastructure/operations assets (Kubernetes manifests, observability config) |
| `scripts/` | Helper scripts (for example, certificate-related setup) |

## 2. Architecture and conventions

Backend services follow Clean Architecture and CQRS patterns:

- `*.Api`: HTTP boundary, configuration, middleware, DI composition.
- `*.Application`: use-cases, MediatR handlers, validators, orchestration.
- `*.Domain`: domain model, business rules, aggregates/value objects.
- `*.Infrastructure`: persistence, messaging, integration adapters.

### Core conventions

1. Keep domain rules in `Domain` and application orchestration in `Application`.
2. Avoid leaking infrastructure concerns into domain/application layers.
3. Reuse `Shared/` extensions and abstractions before adding duplicate implementations.
4. Keep each service independent by bounded context; share only stable cross-cutting building blocks.

## 3. Toolchain and dependencies

### .NET

- SDK is pinned in `global.json` (`10.0.301`, roll-forward by feature).
- Central package versions are managed in `Directory.Packages.props`.

### Frontend

- Angular 21 with npm (`Frontend/Angular/package.json`).
- Common scripts include `start`, `start:https`, `build`, `test`, and `lint`.
- PDF export infrastructure is available through `Frontend/Angular/src/app/shared/services/pdf-export.service.ts` (based on `jspdf` + `jspdf-autotable`) using `TableExportTemplateDto` + table `pdfRender` column hooks.

### Frontend PDF export usage

Use this path to add PDF export on list screens with minimal duplication:

1. Define export columns using `TableColumn<T>[]` and optional `pdfRender` for computed/translated values.
2. Build a `TableExportTemplateDto<T>` with `fileName`, `title`, `rows`, `columns`, and optional `subtitle`/`orientation`.
3. Call `PdfExportService.exportTable(template)` from a page action (for example, Departments and Module Navigation).

## 4. Local development setup

### Prerequisites

1. .NET SDK matching `global.json`.
2. Node.js/npm compatible with Angular tooling.
3. Docker Desktop (for dependencies and containerized runs).

### Environment bootstrap

1. Copy `.env.example` to `.env` and adjust values.
2. Ensure required local certificates exist (see `certs/` and frontend cert setup script).
3. Restore dependencies:
   - Backend: `dotnet restore Usm.Inventory.MonoRepo.slnx`
   - Frontend: `npm install` (inside `Frontend/Angular`)

### Typical local run options

1. **Backend + infrastructure via Docker Compose**
   - `docker compose up -d`
2. **Frontend local dev server**
   - `npm start` (from `Frontend/Angular`)
3. **HTTPS frontend local dev**
   - `npm run start:https` (from `Frontend/Angular`)

## 5. Build, test, and quality workflow

### Backend

- Build: `dotnet build Usm.Inventory.MonoRepo.slnx`
- Test: `dotnet test Usm.Inventory.MonoRepo.slnx`

### Frontend

- Build: `npm run build`
- Unit tests: `npm test`
- Lint: `npm run lint`

### Expected contributor workflow

1. Build and test the area you changed first.
2. Run broader validation when touching shared libraries, contracts, or bootstrapping code.
3. Keep commits scoped by concern (do not bundle unrelated refactors).

## 6. Observability and operations surface

The platform integrates OpenTelemetry + Grafana/Loki/Prometheus/Jaeger in local and platform workflows.

### Key local observability components (from `docker-compose.yml`)

- Grafana (`:3000`)
- Loki (`:3100`)
- Prometheus (`:9090`)
- OTEL Collector (`:4317`, `:4318`, `:8889`)
- Jaeger UI (`:16686`)

### Maintenance notes

1. Treat telemetry configuration as a shared platform concern.
2. Update collector pipelines and dashboards together when changing service metrics/log schema.
3. Keep log fields structured and consistent across services.

## 7. CI/CD and deployment alignment

`azure-pipelines.yml` includes staged workflows for:

1. Build and test.
2. Container image build/push (on `master`).
3. AKS deployment.

Kubernetes manifests live under `Platform/Kubernetes/` and include namespace, observability stack, and workloads.

For full Azure deployment runbook (frontend, backend microservices, gateway, telemetry, ingress, and rollout sequence), see [Azure Cloud Deployment Guide](azure-cloud-deployment-guide.md).

## 8. Adding or evolving a service (maintenance checklist)

Use this checklist when introducing a new bounded context or making major structural changes:

1. Create/align project layers: `Api`, `Application`, `Domain`, `Infrastructure`.
2. Wire shared host defaults and observability through existing shared extensions.
3. Add containerization updates (`Dockerfile`, `docker-compose.yml` service entry, required env vars).
4. Add gateway routing/proxy updates in `Gateway/ApiGateway`.
5. Add health checks and readiness strategy.
6. Add tests (unit/integration as appropriate) and include them in standard pipeline runs.
7. Update docs (`README.md`, this wiki page, and service-specific notes if needed).

## 9. Security and configuration practices

1. Never commit secrets; use `.env` and secure runtime secret stores.
2. Keep certificate files and keys out of source control unless intentionally public/test fixtures.
3. Review authentication and authorization impact for any endpoint or policy change.
4. Keep dependency versions current through central package/version management.

### Authentication maintenance (CAC, FIDO2, password + refresh token)

Use this checklist for ongoing auth operations:

1. **CAC root certificate lifecycle**
   - Generate a new CAC root CA with:
     - `powershell -ExecutionPolicy Bypass -File ./scripts/gen-cac-root-ca.ps1`
   - Distribute and trust only `cac-root-ca.crt` on client/test machines.
   - Keep `cac-root-ca.key` protected/offline and rotate immediately if exposed.
2. **FIDO2 configuration integrity**
   - Ensure `Fido2:RpId`, `Fido2:RpName`, and `Fido2:Origin` match deployed frontend origin(s).
   - Re-validate FIDO2 login after origin, DNS, or ingress TLS changes.
3. **Password and refresh-token flow**
   - Keep refresh-token lifetime aligned with policy and incident response requirements.
   - Verify login endpoints and `/connect/token` refresh flow whenever OpenIddict settings are changed.
4. **Routine verification**
   - Test all three sign-in methods (password, CAC, FIDO2) after auth-related deployments.
   - Confirm refresh-token renewal works and expired/invalid refresh tokens are rejected.

## 10. Troubleshooting quick guide

### Common local issues

| Symptom | Likely cause | Action |
| --- | --- | --- |
| Service cannot connect to Postgres/RabbitMQ | Container not healthy or wrong `.env` values | Check `docker compose ps` and `.env` connection settings |
| Frontend HTTPS startup fails | Missing/invalid local cert files | Re-run cert setup and verify paths in frontend scripts |
| Gateway route returns upstream error | Service not running or route config mismatch | Verify target service health and gateway route mapping |
| No telemetry data in Grafana/Jaeger | OTEL collector or exporters misconfigured | Verify collector service and endpoint configuration |

## 11. Documentation ownership

When architecture, environment setup, CI/CD flow, or service topology changes:

1. Update this page in the same PR.
2. Update `README.md` if onboarding impact exists.
3. Add service-local docs when change complexity warrants deep service guidance.

Keeping this page accurate is part of the definition of done for structural and operational changes.
