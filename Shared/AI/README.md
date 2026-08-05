# Shared.AI

Shared.AI provides the monorepo's reusable AI abstractions and the persistent Python integration layer.

## Features

- persistent Python worker bridge over `STDIN`/`STDOUT`
- typed embedding, classification, and NER wrappers
- DI registration for ASP.NET Core
- health checks and startup pre-warming

## Projects

- `Shared/AI` - core abstractions and infrastructure
- `Samples/PythonBridgeDemo` - minimal API demo for Python integration
- `PythonAIEngine` - containerized gRPC inference engine
- `Samples/AIEngineDemo` - gRPC client demo for the engine

## Quick start

```csharp
builder.Services.AddPythonAI(builder.Configuration, sectionName: "PythonAI");
```

Then inject `TransformersWrapper` or `spaCyWrapper`.

## Python worker

The worker lives under `Shared/AI/python_worker/worker.py` and is launched as a persistent process.

## Documentation

- `docs/ai-python-integration.md`
- `docs/grpc-ai-integration.md`
