from __future__ import annotations

import asyncio
import importlib
import time
from dataclasses import dataclass
from typing import Any, Awaitable, Callable, Dict, Tuple


@dataclass(slots=True)
class LoadedModel:
    value: Any
    loaded_at: float
    load_ms: int


class WorkerState:
    def __init__(self, config):
        self.config = config
        self._embedding_models: Dict[str, LoadedModel] = {}
        self._pipeline_models: Dict[Tuple[str, str], LoadedModel] = {}
        self._spacy_models: Dict[str, LoadedModel] = {}
        self._custom_functions: Dict[str, LoadedModel] = {}
        self._embedding_locks: Dict[str, asyncio.Lock] = {}
        self._pipeline_locks: Dict[Tuple[str, str], asyncio.Lock] = {}
        self._spacy_locks: Dict[str, asyncio.Lock] = {}
        self._custom_locks: Dict[str, asyncio.Lock] = {}

    async def get_embedding_model(self, model: str):
        return await self._get_or_load(
            model,
            self._embedding_models,
            self._embedding_locks,
            self._load_embedding_model,
        )

    async def get_pipeline(self, task: str, model: str):
        return await self._get_or_load(
            (task, model),
            self._pipeline_models,
            self._pipeline_locks,
            lambda: self._load_pipeline(task, model),
        )

    async def get_spacy_model(self, model: str):
        return await self._get_or_load(
            model,
            self._spacy_models,
            self._spacy_locks,
            self._load_spacy_model,
        )

    async def get_custom_function(self, operation: str):
        return await self._get_or_load(
            operation,
            self._custom_functions,
            self._custom_locks,
            lambda: self._load_custom_function(operation),
        )

    async def warmup(self):
        for model in self.config.models:
            await self._warmup_model(model.name)

    async def _warmup_model(self, model: str):
        if model.startswith("en_core_web_"):
            await self.get_spacy_model(model)
        elif "sentence-transformers" in model or "embed" in model.lower() or "minilm" in model.lower():
            await self.get_embedding_model(model)
        else:
            await self.get_pipeline("sentiment-analysis", model)

    async def _get_or_load(self, key, cache, locks, factory):
        if key in cache:
            return cache[key].value

        lock = locks.get(key)
        if lock is None:
            lock = asyncio.Lock()
            locks[key] = lock

        async with lock:
            if key in cache:
                return cache[key].value

            started = time.perf_counter()
            value = await factory()
            elapsed_ms = int((time.perf_counter() - started) * 1000)
            cache[key] = LoadedModel(value=value, loaded_at=time.time(), load_ms=elapsed_ms)
            return value

    async def _load_embedding_model(self, model: str):
        from sentence_transformers import SentenceTransformer

        return SentenceTransformer(model)

    async def _load_pipeline(self, task: str, model: str):
        from transformers import pipeline

        return pipeline(task, model=model)

    async def _load_spacy_model(self, model: str):
        import spacy

        return spacy.load(model)

    async def _load_custom_function(self, operation: str):
        for function in self.config.custom_functions:
            if function.operation == operation:
                module = importlib.import_module(function.module)
                target = getattr(module, function.function)
                return target

        raise KeyError(f"Unknown custom operation: {operation}")

