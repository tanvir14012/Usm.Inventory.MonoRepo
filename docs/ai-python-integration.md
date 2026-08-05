# AI and Python integration

This solution integrates .NET with a persistent Python runtime for ML workloads.

## Architecture

- `Shared/AI` exposes the .NET abstraction layer.
- `PersistentPythonBridge` keeps a Python worker process alive.
- `python_worker/worker.py` reads single-line JSON requests from `STDIN` and writes JSON responses to `STDOUT`.
- `TransformersWrapper` and `spaCyWrapper` are thin DI-friendly facades over the bridge.

## Runtime model

- The worker loads models lazily and caches them in memory.
- Requests are routed by operation name and model name.
- Communication uses JSON payloads only.
- No temp Python scripts are generated at runtime.

## Configuration

`Shared/AI` accepts a `PythonAI` config section:

```json
{
  "PythonAI": {
    "PythonExecutablePath": "python",
    "WorkerRootPath": "Shared/AI/python_worker",
    "Pools": [
      {
        "Name": "cpu",
        "Role": "Cpu",
        "WorkerCount": 1,
        "MaxConcurrentRequestsPerWorker": 4,
        "Models": [
          "sentence-transformers/all-MiniLM-L6-v2",
          "en_core_web_sm"
        ]
      }
    ]
  }
}
```

## Usage

```csharp
builder.Services.AddPythonAI(builder.Configuration, sectionName: "PythonAI");
```

Then inject:

- `PersistentPythonBridge`
- `TransformersWrapper`
- `spaCyWrapper`

## Demo endpoints

See `Samples/PythonBridgeDemo` for minimal embedding and entity extraction endpoints.
