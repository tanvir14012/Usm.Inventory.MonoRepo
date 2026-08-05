# PythonAIEngine

This container hosts the gRPC-based Python AI engine used by `Samples/AIEngineDemo`.

## What it does

- boots a long-running Python AI service
- serves gRPC requests for AI tasks
- pre-warms configured models on startup
- runs as a dedicated container in `PythonAIEngine/docker-compose.yml`

## Local run

```bash
docker compose -f PythonAIEngine/docker-compose.yml up --build
```

The demo API expects:

- `AI_ENGINE_ENDPOINT=http://python-ai-engine:50051`

## Notes

The container is the right place for model-heavy inference workloads when you want isolation from the main .NET services.
