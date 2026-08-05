# gRPC AI integration

The repo already includes a gRPC-based AI engine under `PythonAIEngine` and a .NET client demo in `Samples/AIEngineDemo`.

## Flow

1. `PythonAIEngine` starts the Python inference service in a container.
2. `Samples/AIEngineDemo` connects to it with `Shared.AI.EngineClient`.
3. Requests are executed and streamed over gRPC instead of spawning per-call processes.

## Demo project

`Samples/AIEngineDemo/Program.cs` exposes:

- `POST /tasks/execute`
- `POST /tasks/stream`

## Compose stack

Use `PythonAIEngine/docker-compose.yml` to start the engine and the demo together.
