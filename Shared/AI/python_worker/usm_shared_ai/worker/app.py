from __future__ import annotations

import asyncio
from dataclasses import asdict
import json
import os
import platform
import tempfile
import signal
import sys
import traceback
from typing import Any, Dict

from .config import as_serializable, load_bootstrap_config
from .handlers import CustomFunctionHandler, EmbeddingHandler, SpacyHandler, TransformersHandler
from .protocol import Error, Request, Response
from .state import WorkerState


class WorkerApp:
    def __init__(self) -> None:
        self.config = load_bootstrap_config()
        self.state = WorkerState(self.config)
        self.handlers = [
            EmbeddingHandler(),
            TransformersHandler(),
            SpacyHandler(),
            CustomFunctionHandler(),
        ]
        self._handler_map = self._build_handler_map()
        self._stdout_lock = asyncio.Lock()
        self._stop_event = asyncio.Event()
        self._tasks: set[asyncio.Task[None]] = set()

    async def run(self) -> None:
        self._validate_python_version()
        await self._warmup()
        self._mark_ready()
        self._install_signal_handlers()
        try:
            await self._main_loop()
        finally:
            self._clear_ready()

    async def _main_loop(self) -> None:
        loop = asyncio.get_running_loop()

        while not self._stop_event.is_set():
            line = await asyncio.to_thread(sys.stdin.readline)
            if not line:
                break

            line = line.strip()
            if not line:
                continue

            task = loop.create_task(self._handle_line(line))
            self._tasks.add(task)
            task.add_done_callback(self._tasks.discard)

        if self._tasks:
            await asyncio.gather(*self._tasks, return_exceptions=True)

    async def _handle_line(self, line: str) -> None:
        started = asyncio.get_running_loop().time()
        payload: Dict[str, Any] = {}
        try:
            payload = json.loads(line)
            request = Request(
                request_id=payload["requestId"],
                operation=str(payload["operation"]).lower(),
                model=payload.get("model"),
                parameters=payload.get("parameters") or {},
                correlation_id=payload.get("correlationId"),
                worker_role=payload.get("workerRole"),
                stream=payload.get("stream"),
                protocol_version=payload.get("protocolVersion"),
            )
            response = await self._dispatch(request)
        except Exception as exc:
            response = Response(
                request_id=payload.get("requestId", "") if isinstance(payload, dict) else "",
                success=False,
                error=Error(
                    code=type(exc).__name__,
                    message=str(exc),
                    details=repr(exc),
                    stack_trace=traceback.format_exc(),
                ),
                worker_id=self.config.worker_id,
            )

        response.duration_ms = int((asyncio.get_running_loop().time() - started) * 1000)
        await self._write_response(response)

    async def _dispatch(self, request: Request) -> Response:
        if request.operation == "heartbeat":
            return Response(
                request_id=request.request_id,
                success=True,
                result={
                    "operation": "heartbeat",
                    "workerId": self.config.worker_id,
                    "poolName": self.config.pool_name,
                    "role": self.config.role,
                    "protocolVersion": self.config.protocol_version,
                    "pythonVersion": platform.python_version(),
                    "pid": os.getpid(),
                },
                worker_id=self.config.worker_id,
                correlation_id=request.correlation_id,
            )

        if request.operation == "shutdown":
            self._stop_event.set()
            return Response(
                request_id=request.request_id,
                success=True,
                result={"operation": "shutdown"},
                worker_id=self.config.worker_id,
                correlation_id=request.correlation_id,
            )

        if request.protocol_version is not None and request.protocol_version != self.config.protocol_version:
            raise ValueError(f"Unsupported protocol version: {request.protocol_version}")

        handler = self._handler_map.get(request.operation)
        if handler is None:
            raise ValueError(f"Unsupported operation: {request.operation}")

        result = await handler.handle(request, self.state)
        return Response(
            request_id=request.request_id,
            success=True,
            result=as_serializable(result),
            worker_id=self.config.worker_id,
            correlation_id=request.correlation_id,
        )

    async def _write_response(self, response: Response) -> None:
        payload = json.dumps(_camelize(as_serializable(asdict(response))), separators=(",", ":"), ensure_ascii=False)
        async with self._stdout_lock:
            sys.stdout.write(payload + "\n")
            sys.stdout.flush()

    def _build_handler_map(self):
        mapping: Dict[str, Any] = {}
        for handler in self.handlers:
            for operation in handler.operations:
                mapping[operation] = handler
        return mapping

    async def _warmup(self) -> None:
        if self.config.warmup_models:
            await self.state.warmup()

    def _validate_python_version(self) -> None:
        minimum = tuple(int(part) for part in self.config.minimum_python_version.split(".")[:2])
        current = sys.version_info[:2]
        if current < minimum:
            raise RuntimeError(
                f"Python {self.config.minimum_python_version}+ is required, but {platform.python_version()} is running."
            )

    def _install_signal_handlers(self) -> None:
        for sig in (signal.SIGINT, signal.SIGTERM):
            try:
                asyncio.get_running_loop().add_signal_handler(sig, self._stop_event.set)
            except (NotImplementedError, RuntimeError):
                pass

    def _mark_ready(self) -> None:
        ready_file = os.getenv("USM_SHARED_AI_READY_FILE", os.path.join(tempfile.gettempdir(), "usm-shared-ai.ready"))
        with open(ready_file, "w", encoding="utf-8") as handle:
            handle.write(self.config.worker_id)

    def _clear_ready(self) -> None:
        ready_file = os.getenv("USM_SHARED_AI_READY_FILE", os.path.join(tempfile.gettempdir(), "usm-shared-ai.ready"))
        try:
            os.remove(ready_file)
        except FileNotFoundError:
            pass


def _camelize(value):
    if isinstance(value, dict):
        return { _to_camel_case(key): _camelize(item) for key, item in value.items() }
    if isinstance(value, list):
        return [_camelize(item) for item in value]
    return value


def _to_camel_case(value: str) -> str:
    if "_" not in value:
        return value
    parts = value.split("_")
    return parts[0] + "".join(part[:1].upper() + part[1:] for part in parts[1:])


async def main() -> None:
    worker = WorkerApp()
    await worker.run()
