# Development Guide

## Code formatting baseline (Visual Studio 2026 + VS Code)

- Backend/.NET conventions are centralized in the repository-root `.editorconfig`, aligned with Microsoft/.NET Core coding-style guidance.
- Frontend/Angular conventions are enforced by `Frontend/Angular/.editorconfig`, `Frontend/Angular/.prettierrc`, and `Frontend/Angular/.vscode/settings.json`.

Use these commands before pushing:

```bash
dotnet format Usm.Inventory.MonoRepo.slnx
cd Frontend/Angular
npm run format
```

## Local stack bootstrap

1. Copy environment file:
   ```bash
   cp .env.example .env
   ```
2. Start the complete stack:
   ```bash
   docker compose up -d --build
   ```
3. Verify core dependencies:
   ```bash
   docker compose ps
   docker compose logs -f postgres-primary redis localstack
   ```

## Local endpoints

- Frontend: `https://localhost:8443`
- Gateway: `http://localhost:8080`
- Redis: `localhost:6379`
- PostgreSQL primary: `localhost:5432`
- PostgreSQL replica: `localhost:5433`
- Local CDN emulator (LocalStack CloudFront API): `http://localhost:4566`
- Grafana: `http://localhost:3000`
- Prometheus: `http://localhost:9090`

## Useful commands

1. Rebuild only changed services:
   ```bash
   docker compose build api-gateway identity-api frontend
   ```
2. Restart a subset:
   ```bash
   docker compose up -d api-gateway identity-api
   ```
3. Follow gateway + ingress logs:
   ```bash
   docker compose logs -f nginx api-gateway
   ```

## Environment override strategy

- Use `.env` for local defaults.
- Use `docker-compose.override.yml` for machine-specific overrides (ports, feature flags).
- Keep secrets out of git; export secure variables from your shell or local secret manager.

Common overrides:

```bash
ASPNETCORE_ENVIRONMENT=Development
POSTGRES_PASSWORD=localStrongPassword
OTLP_ENDPOINT=http://otel-collector:4317
```

## AKS deployment commands (manual fallback)

```bash
kubectl apply -f Platform/Kubernetes/00-namespace.yaml
kubectl apply -f Platform/Kubernetes/01-configmap.yaml
kubectl apply -f Platform/Kubernetes/02-secret.yaml
kubectl apply -f Platform/Kubernetes/03-backend-workloads.yaml
kubectl apply -f Platform/Kubernetes/04-frontend-workloads.yaml
kubectl apply -f Platform/Kubernetes/05-hpa.yaml
kubectl apply -f Platform/Kubernetes/06-ingress.yaml
kubectl apply -f Platform/Kubernetes/07-observability.yaml
```

## Troubleshooting

1. **Pod CrashLoopBackOff**
   - Check probe endpoint (`/health`) and app startup logs.
   - Confirm secret/config keys are loaded.
2. **Ingress 502**
   - Validate backend service endpoints:
     ```bash
     kubectl -n usm-platform get ep gateway frontend
     ```
3. **Slow responses or throttling**
   - Inspect HPA metrics and ingress rate-limit annotations.
4. **Telemetry missing**
   - Confirm OTLP endpoint env and `otel-collector` service reachability.
   - Validate Prometheus targets in `/targets`.
