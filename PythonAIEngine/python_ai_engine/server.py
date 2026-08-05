from __future__ import annotations

import asyncio
import json
import logging
import signal
from contextlib import suppress
from typing import Any, AsyncIterator, Dict

import grpc
from grpc_health.v1 import health_pb2, health_pb2_grpc
from grpc_health.v1.health import HealthServicer

from .config import EngineConfig, load_config, to_json
from .model_manager import ModelManager
from .tasks import TaskContext, build_default_registry

from .generated import ai_engine_pb2, ai_engine_pb2_grpc


class AIEngineService(ai_engine_pb2_grpc.AIEngineServiceServicer):
    def __init__(self, config: EngineConfig, registry, model_manager: ModelManager, logger: logging.Logger) -> None:
        self._config = config
        self._registry = registry
        self._model_manager = model_manager
        self._logger = logger

    async def ExecuteTask(self, request, context):  # noqa: N802
        started = asyncio.get_running_loop().time()
        task_context = self._build_context(request, context)
        try:
            handler = self._registry.resolve(request.task_type)
            result = await handler.execute(task_context)
            execution_time_ms = int((asyncio.get_running_loop().time() - started) * 1000)
            return ai_engine_pb2.TaskResponse(
                status="success",
                result_json=to_json(result),
                metadata=dict(request.metadata),
                execution_time_ms=execution_time_ms,
            )
        except Exception as exc:
            self._logger.exception("ExecuteTask failed for %s", request.task_type)
            execution_time_ms = int((asyncio.get_running_loop().time() - started) * 1000)
            return ai_engine_pb2.TaskResponse(
                status="error",
                result_json=to_json({"error": type(exc).__name__, "message": str(exc)}),
                metadata={**dict(request.metadata), "error": type(exc).__name__},
                execution_time_ms=execution_time_ms,
            )

    async def StreamTask(self, request, context):  # noqa: N802
        started = asyncio.get_running_loop().time()
        task_context = self._build_context(request, context)
        sequence_id = 0
        try:
            handler = self._registry.resolve(request.task_type)
            async for event in handler.stream(task_context):
                sequence_id = int(event.get("sequence_id", sequence_id + 1))
                yield ai_engine_pb2.TaskStreamResponse(
                    event_type=str(event.get("event_type", "progress")),
                    data_json=str(event.get("data_json", "")),
                    sequence_id=sequence_id,
                )
        except Exception as exc:
            self._logger.exception("StreamTask failed for %s", request.task_type)
            yield ai_engine_pb2.TaskStreamResponse(
                event_type="error",
                data_json=to_json({"error": type(exc).__name__, "message": str(exc)}),
                sequence_id=sequence_id + 1,
            )
        finally:
            self._logger.debug(
                "StreamTask completed for %s in %sms",
                request.task_type,
                int((asyncio.get_running_loop().time() - started) * 1000),
            )

    def _build_context(self, request, grpc_context) -> TaskContext:
        return TaskContext(
            task_type=request.task_type,
            payload=json.loads(request.payload_json or "{}"),
            metadata=dict(request.metadata),
            model_manager=self._model_manager,
            config=self._config,
            logger=self._logger,
        )


async def serve() -> None:
    config = load_config()
    logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s %(message)s")
    logger = logging.getLogger("python_ai_engine")

    registry = build_default_registry()
    model_manager = ModelManager(config)
    health = HealthServicer()
    server = grpc.aio.server(
        options=[
            ("grpc.max_send_message_length", 32 * 1024 * 1024),
            ("grpc.max_receive_message_length", 32 * 1024 * 1024),
        ],
        maximum_concurrent_rpcs=config.max_concurrent_rpcs,
    )

    ai_engine_pb2_grpc.add_AIEngineServiceServicer_to_server(
        AIEngineService(config, registry, model_manager, logger),
        server,
    )
    health_pb2_grpc.add_HealthServicer_to_server(health, server)
    health.set("", health_pb2.HealthCheckResponse.NOT_SERVING)
    health.set("ai.engine.v1.AIEngineService", health_pb2.HealthCheckResponse.NOT_SERVING)

    listen_address = f"{config.host}:{config.port}"
    server.add_insecure_port(listen_address)

    await server.start()
    logger.info("AI engine listening on %s", listen_address)

    try:
        if config.warmup_models:
            await model_manager.warmup()
        health.set("", health_pb2.HealthCheckResponse.SERVING)
        health.set("ai.engine.v1.AIEngineService", health_pb2.HealthCheckResponse.SERVING)
        await server.wait_for_termination()
    finally:
        health.set("", health_pb2.HealthCheckResponse.NOT_SERVING)
        health.set("ai.engine.v1.AIEngineService", health_pb2.HealthCheckResponse.NOT_SERVING)
        await server.stop(grace=5)


def main() -> None:
    with suppress(KeyboardInterrupt):
        asyncio.run(serve())
