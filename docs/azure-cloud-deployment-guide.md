# Azure Cloud Deployment Guide (Frontend, Backend, Telemetry)

This guide documents end-to-end deployment of the USM Inventory platform to Azure, covering frontend, API gateway, backend microservices, and observability.

## 1. Scope and deployment model

- **Container registry:** Azure Container Registry (ACR)
- **Runtime:** Azure Kubernetes Service (AKS)
- **CI/CD:** Azure DevOps pipeline (`azure-pipelines.yml`)
- **Telemetry stack in-cluster:** OpenTelemetry Collector + Prometheus + Loki + Grafana

## 2. Component map

| Layer | Repo path | Artifact |
| --- | --- | --- |
| Frontend SPA + TLS/proxy | `Frontend/Dockerfile`, `Frontend/nginx/nginx.conf` | `frontend-nginx` image |
| API gateway (YARP) | `Gateway/ApiGateway` | `api-gateway` image |
| Backend APIs | `Services/*/src/*Api` | One image per service |
| AKS manifests | `Platform/Kubernetes/` | Namespace, observability, workloads |
| CI/CD definition | `azure-pipelines.yml` | Build/test, image push, AKS deploy |

## 3. Prerequisites

1. Azure subscription with permissions for AKS/ACR/Networking.
2. Azure DevOps project and service connections:
   - `usm-acr-connection`
   - `usm-aks-connection`
3. Azure CLI + kubectl installed for operator access.
4. DNS + TLS plan for public frontend URL.
5. Runtime dependencies provisioned (at minimum Postgres and RabbitMQ endpoints reachable from AKS, if not deployed in-cluster).

## 4. Azure resource baseline

Create/provision these resources before first deployment:

1. Resource Group (for example `rg-usm-prod`)
2. ACR (for example `usmregistry.azurecr.io`)
3. AKS cluster (for example `aks-usm-prod`)
4. AKS namespace: `usm-platform`
5. Ingress controller (NGINX or AGIC) for public frontend/gateway routing
6. Secret store strategy (Azure Key Vault + CSI driver recommended)

## 5. CI/CD behavior in this repository

`azure-pipelines.yml` has three stages:

1. **BuildAndTest:** restore, build, test `Usm.Inventory.MonoRepo.slnx`
2. **BuildAndPushImages:** builds/pushes backend API images + `api-gateway` to ACR with `$(Build.BuildId)` tag
3. **DeployToAks:** injects `__ACR_LOGIN_SERVER__` and `__IMAGE_TAG__`, then applies:
   - `Platform/Kubernetes/namespace.yaml`
   - `Platform/Kubernetes/observability.yaml`
   - `Platform/Kubernetes/workloads.yaml`

## 6. Deploying frontend on Azure (recommended production pattern)

The frontend runtime is NGINX (`Frontend/Dockerfile`) and expects `/api/*` to be proxied to gateway.

### 6.1 Build and push frontend image

Add `frontend-nginx|Frontend/Dockerfile` to the `services` array in `BuildAndPushImages`, or build/push manually:

```bash
docker build -f Frontend/Dockerfile -t <acr-login-server>/frontend-nginx:<tag> .
docker push <acr-login-server>/frontend-nginx:<tag>
```

### 6.2 Add AKS frontend workload

Add frontend `Deployment` + `Service` to `Platform/Kubernetes/workloads.yaml` using:

- container port `80` (or `443` if terminating TLS in pod)
- image `<acr-login-server>/frontend-nginx:<tag>`
- service name `frontend`

### 6.3 Add ingress routes

Ingress should route:

- `/` → `frontend` service
- `/api` → `gateway` service

This preserves `environment.prod.ts` behavior (`apiGatewayUrl: '/api'`).

## 7. Deploying backend microservices

Current pipeline image build list includes:

- `identity-api`, `iam-api`, `administration-api`, `storehouse-api`, `documentshare-api`, `issuereceipt-api`, `trafficsecurity-api`, `salvage-api`, `repairmaintenance-api`, `budgetplanning-api`, `communication-api`, `inspectorate-api`, `reporting-api`, `procurement-api`, and `api-gateway`.

`Gateway/ApiGateway/appsettings.json` already contains routes/clusters for these services, so AKS service names should match those cluster destination hostnames (for example `identity-api`, `iam-api`, etc.).

> The current `Platform/Kubernetes/workloads.yaml` contains `identity-api` and `gateway` only. Add remaining API `Deployment`/`Service` objects for a complete production rollout.

## 8. Telemetry deployment on AKS

`Platform/Kubernetes/observability.yaml` deploys:

- OpenTelemetry Collector (OTLP gRPC 4317 / HTTP 4318)
- Prometheus (9090)
- Loki (3100)
- Grafana (3000)

Runtime config in `workloads.yaml` sets:

- `Observability__OtlpEndpoint=http://otel-collector.usm-platform.svc.cluster.local:4317`
- `Observability__LokiEndpoint=http://loki.usm-platform.svc.cluster.local:3100`

After deployment, validate quickly:

```bash
kubectl -n usm-platform get pods
kubectl -n usm-platform get svc
kubectl -n usm-platform port-forward svc/grafana 3000:3000
```

## 9. Secrets and configuration

Do not store production secrets in manifests. Store and mount/inject:

1. Postgres connection string
2. RabbitMQ credentials
3. OpenIddict certificate password and encryption keys
4. Any identity/OIDC secrets

Recommended: Azure Key Vault + CSI Secret Store provider, then reference as environment variables/secrets in workload manifests.

## 10. First-time deployment sequence

1. Configure ACR/AKS service connections in Azure DevOps.
2. Prepare `Platform/Kubernetes/workloads.yaml` for all required APIs + frontend.
3. Merge to `master` to trigger pipeline.
4. Confirm image pushes in ACR.
5. Confirm AKS rollout (`kubectl -n usm-platform get deploy,pods,svc,ingress`).
6. Smoke test:
   - frontend UI is reachable
   - `/api/identity/*` and another non-identity route return through gateway
   - telemetry visible in Grafana/Prometheus/Jaeger equivalent setup

## 11. Operational notes

- Keep `Gateway/ApiGateway/appsettings.json` routes in sync with deployed service names.
- Keep telemetry endpoint variables consistent across all services.
- Use rolling updates and pinned image tags (already based on `Build.BuildId`) for traceable deployments.
- Update this guide when adding/removing services or changing ingress/security topology.

