from __future__ import annotations

import abc
import ast
import asyncio
import importlib
from dataclasses import dataclass
from typing import Any, AsyncIterator, Dict, Iterable, List, Optional, Sequence

from .config import to_json
from .model_manager import IndexedDocument, ModelManager


@dataclass(slots=True)
class TaskContext:
    task_type: str
    payload: Dict[str, Any]
    metadata: Dict[str, str]
    model_manager: ModelManager
    config: Any
    logger: Any


class BaseTaskHandler(abc.ABC):
    task_types: Sequence[str] = ()

    def supports(self, task_type: str) -> bool:
        return task_type in self.task_types

    async def execute(self, context: TaskContext) -> Any:
        raise NotImplementedError(f"{self.__class__.__name__} does not support ExecuteTask.")

    async def stream(self, context: TaskContext) -> AsyncIterator[Dict[str, Any]]:
        raise NotImplementedError(f"{self.__class__.__name__} does not support StreamTask.")


class TaskRegistry:
    def __init__(self) -> None:
        self._handlers: List[BaseTaskHandler] = []

    def register(self, handler: BaseTaskHandler) -> None:
        self._handlers.append(handler)

    def resolve(self, task_type: str) -> BaseTaskHandler:
        for handler in reversed(self._handlers):
            if handler.supports(task_type):
                return handler
        raise KeyError(f"Unknown task type: {task_type}")


class LlmStreamHandler(BaseTaskHandler):
    task_types = ("llm_stream",)

    async def stream(self, context: TaskContext) -> AsyncIterator[Dict[str, Any]]:
        prompt = str(context.payload.get("prompt", ""))
        backend = context.payload.get("backend")
        model_name = context.payload.get("model")

        for sequence_id, token in enumerate(
            context.model_manager.stream_tokens(prompt, backend=backend, model_name=model_name),
            start=1,
        ):
            yield {"event_type": "token", "data_json": to_json(token), "sequence_id": sequence_id}
            await asyncio.sleep(0)

        yield {
            "event_type": "complete",
            "data_json": to_json({"task_type": context.task_type, "message": "stream complete"}),
            "sequence_id": sequence_id + 1 if "sequence_id" in locals() else 1,
        }


class RagSearchHandler(BaseTaskHandler):
    task_types = ("rag_search",)

    async def execute(self, context: TaskContext) -> Any:
        query = str(context.payload.get("query", ""))
        top_k = int(context.payload.get("top_k", 5))
        corpus = str(context.payload.get("corpus", "default"))
        backend = context.payload.get("backend")
        model_name = context.payload.get("model")

        documents = context.payload.get("documents") or []
        if documents:
            indexed: List[IndexedDocument] = []
            for index, document in enumerate(documents):
                text = str(document.get("text", ""))
                metadata = dict(document.get("metadata", {}))
                indexed.append(
                    IndexedDocument(
                        document_id=str(document.get("document_id", f"doc-{index}")),
                        text=text,
                        metadata=metadata,
                        embedding=context.model_manager.embed(text, backend=backend, model_name=model_name),
                    )
                )
            context.model_manager.vector_store.upsert(corpus, indexed)

        query_embedding = context.model_manager.embed(query, backend=backend, model_name=model_name)
        matches = context.model_manager.vector_store.search(corpus, query_embedding, top_k=top_k)
        return {"query": query, "matches": matches}


class PythonExecHandler(BaseTaskHandler):
    task_types = ("python_exec",)

    async def execute(self, context: TaskContext) -> Any:
        if not context.config.allow_arbitrary_code and context.payload.get("code"):
            raise PermissionError("Arbitrary code execution is disabled.")

        if "module" in context.payload and "callable" in context.payload:
            module = importlib.import_module(str(context.payload["module"]))
            fn = getattr(module, str(context.payload["callable"]))
            args = context.payload.get("args", [])
            kwargs = context.payload.get("kwargs", {})
            if asyncio.iscoroutinefunction(fn):
                return await fn(*args, **kwargs)
            return fn(*args, **kwargs)

        code = str(context.payload.get("code", ""))
        globals_dict = _safe_globals()
        locals_dict = {"payload": context.payload, "metadata": context.metadata, "result": None}

        def _run_exec() -> Any:
            compiled = compile(code, "<python_exec>", "exec")
            exec(compiled, globals_dict, locals_dict)
            return locals_dict.get("result")

        return await asyncio.to_thread(_run_exec)


def build_default_registry() -> TaskRegistry:
    registry = TaskRegistry()
    registry.register(LlmStreamHandler())
    registry.register(RagSearchHandler())
    registry.register(PythonExecHandler())
    return registry


def _safe_globals() -> Dict[str, Any]:
    allowed_builtins = {
        "abs": abs,
        "all": all,
        "any": any,
        "bool": bool,
        "dict": dict,
        "enumerate": enumerate,
        "float": float,
        "int": int,
        "len": len,
        "list": list,
        "max": max,
        "min": min,
        "range": range,
        "set": set,
        "str": str,
        "sum": sum,
        "tuple": tuple,
        "zip": zip,
    }
    return {"__builtins__": allowed_builtins}

