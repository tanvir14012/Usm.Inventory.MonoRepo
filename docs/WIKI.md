# USM Inventory Platform Wiki

## Engineering formatting standards

- Backend formatting is governed by the root `.editorconfig` using Microsoft/.NET Core style defaults for C# and solution-level files.
- Angular frontend formatting is governed by `Frontend/Angular/.editorconfig` + Prettier + VS Code workspace settings in `Frontend/Angular/.vscode/settings.json`.
- Repository-wide normalization commands:
  - `dotnet format Usm.Inventory.MonoRepo.slnx`
  - `cd Frontend/Angular && npm run format`

## Cloud/AKS Architecture

```text
Developer Commit
      |
      v
Azure DevOps CI (Build/Test/Security)
      |
      v
Container Images (SHA tagged)
      |
      v
Azure Container Registry (ACR)
      |
      v
Azure DevOps CD (Staging -> Production)
      |
      v
AKS (usm-platform namespace)
  +--------------------------------------------------------------+
  | NGINX Ingress + TLS + Rate Limit                             |
  |      |                                                       |
  |      +--> frontend (Angular/Nginx)                           |
  |      +--> gateway (/api/v1,/api/v2)                          |
  |               |                                              |
  |               +--> backend microservices (14 services)       |
  |                                                              |
  | Observability: OpenTelemetry -> Prometheus/Loki -> Grafana   |
  | Data: PostgreSQL Primary/Replica + MongoDB shard policy      |
  +--------------------------------------------------------------+
      |
      v
Azure Front Door + WAF + Edge Cache + Custom Domain TLS
```

## Workload Segmentation

1. `Platform/Kubernetes/03-backend-workloads.yaml` contains gateway and all backend APIs with hardened security contexts and probes.
2. `Platform/Kubernetes/04-frontend-workloads.yaml` contains frontend deployment and service.
3. `Platform/Kubernetes/05-hpa.yaml` provides autoscaling targets (CPU 70%, Memory 80%).
4. `Platform/Kubernetes/06-ingress.yaml` handles TLS, API version routing, CORS, and ingress-level rate limiting.
5. `Platform/Kubernetes/07-observability.yaml` centralizes telemetry collection and scraping.

## Security Controls

- Pod security restricted namespace policy.
- Workload identity service account for Azure federation.
- Containers run non-root with read-only root filesystem and dropped Linux capabilities.
- Ingress and edge WAF rate-limiting.
- NGINX hardened TLS 1.3, HSTS, CSP, and X.509 client-certificate forwarding.

## Delivery Model

- **Build**: .NET restore/build + container image build (SHA tag).
- **Test**: unit/integration tests.
- **Security_Scan**: Trivy image scan before release.
- **Deploy_Staging**: apply manifests, update images, rollout check.
- **Deploy_Prod**: gated production rollout with rollback-on-failure behavior.
