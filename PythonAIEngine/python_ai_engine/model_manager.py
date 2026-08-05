from __future__ import annotations

import asyncio
import math
from threading import Thread
from dataclasses import dataclass
from typing import Any, Dict, Iterable, List, Optional


@dataclass(slots=True)
class IndexedDocument:
    document_id: str
    text: str
    metadata: Dict[str, Any]
    embedding: List[float]


class MemoryVectorStore:
    def __init__(self) -> None:
        self._indexes: Dict[str, Dict[str, IndexedDocument]] = {}

    def upsert(self, corpus: str, documents: Iterable[IndexedDocument]) -> None:
        index = self._indexes.setdefault(corpus, {})
        for document in documents:
            index[document.document_id] = document

    def search(self, corpus: str, query_embedding: List[float], top_k: int = 5) -> List[Dict[str, Any]]:
        index = self._indexes.get(corpus, {})
        ranked: List[Dict[str, Any]] = []
        for document in index.values():
            score = cosine_similarity(query_embedding, document.embedding)
            ranked.append(
                {
                    "document_id": document.document_id,
                    "text": document.text,
                    "metadata": document.metadata,
                    "score": score,
                }
            )

        ranked.sort(key=lambda item: item["score"], reverse=True)
        return ranked[:top_k]


class ModelManager:
    def __init__(self, config) -> None:
        self._config = config
        self._locks: Dict[str, asyncio.Lock] = {}
        self._embedding_models: Dict[str, Any] = {}
        self._llm_models: Dict[str, Any] = {}
        self._vector_store = MemoryVectorStore()

    @property
    def vector_store(self) -> MemoryVectorStore:
        return self._vector_store

    async def warmup(self) -> None:
        for model in self._config.warmups:
            if model.kind.lower() == "embedding":
                await self.get_embedding_model(model.name)
            elif model.kind.lower() == "llm":
                await self.get_llm_model(model.name)

    async def get_embedding_model(self, name: str) -> Any:
        if name in self._embedding_models:
            return self._embedding_models[name]

        lock = self._locks.setdefault(f"embedding:{name}", asyncio.Lock())
        async with lock:
            if name in self._embedding_models:
                return self._embedding_models[name]

            model = self._load_embedding_model(name)
            self._embedding_models[name] = model
            return model

    async def get_llm_model(self, name: str) -> Any:
        if name in self._llm_models:
            return self._llm_models[name]

        lock = self._locks.setdefault(f"llm:{name}", asyncio.Lock())
        async with lock:
            if name in self._llm_models:
                return self._llm_models[name]

            model = self._load_llm_model(name)
            self._llm_models[name] = model
            return model

    def embed(self, text: str, backend: Optional[str] = None, model_name: Optional[str] = None) -> List[float]:
        backend = backend or self._config.default_embedding_backend
        if backend == "simulated":
            return simulated_embedding(text)

        if model_name is None:
            raise RuntimeError("An embedding model name is required for non-simulated backends.")

        model = self._embedding_models.get(model_name)
        if model is None:
            raise RuntimeError(f"Embedding model '{model_name}' is not loaded.")

        vector = model.encode(text)
        if hasattr(vector, "tolist"):
            return [float(item) for item in vector.tolist()]
        return [float(item) for item in vector]

    def generate(self, prompt: str, backend: Optional[str] = None, model_name: Optional[str] = None) -> str:
        backend = backend or self._config.default_llm_backend
        if backend == "simulated":
            return f"Simulated response for: {prompt}"

        if model_name is None:
            raise RuntimeError("An LLM model name is required for non-simulated backends.")

        model_bundle = self._llm_models.get(model_name)
        if model_bundle is None:
            raise RuntimeError(f"LLM model '{model_name}' is not loaded.")

        tokenizer = model_bundle["tokenizer"]
        model = model_bundle["model"]
        inputs = tokenizer(prompt, return_tensors="pt")
        if hasattr(model, "device"):
            device = model.device
            inputs = {key: value.to(device) for key, value in inputs.items()}

        output_tokens = model.generate(**inputs, max_new_tokens=128)
        return tokenizer.decode(output_tokens[0], skip_special_tokens=True)

    def stream_tokens(self, prompt: str, backend: Optional[str] = None, model_name: Optional[str] = None) -> Iterable[str]:
        backend = backend or self._config.default_llm_backend
        if backend == "simulated":
            for token in f"Simulated response for: {prompt}".split():
                yield token
            return

        if model_name is None:
            raise RuntimeError("An LLM model name is required for non-simulated backends.")

        model_bundle = self._llm_models.get(model_name)
        if model_bundle is None:
            raise RuntimeError(f"LLM model '{model_name}' is not loaded.")

        try:
            from transformers import TextIteratorStreamer
        except ImportError as exc:
            raise RuntimeError("transformers is required for streamed LLM backends.") from exc

        tokenizer = model_bundle["tokenizer"]
        model = model_bundle["model"]
        inputs = tokenizer(prompt, return_tensors="pt")
        if hasattr(model, "device"):
            device = model.device
            inputs = {key: value.to(device) for key, value in inputs.items()}

        streamer = TextIteratorStreamer(tokenizer, skip_prompt=True, skip_special_tokens=True)
        thread = Thread(
            target=model.generate,
            kwargs={
                **inputs,
                "streamer": streamer,
                "max_new_tokens": 128,
            },
            daemon=True,
        )
        thread.start()
        for token in streamer:
            yield token
        thread.join(timeout=1)

    def _load_embedding_model(self, name: str) -> Any:
        try:
            from sentence_transformers import SentenceTransformer
        except ImportError as exc:
            raise RuntimeError(
                "sentence-transformers is required for embedding backends other than 'simulated'."
            ) from exc

        return SentenceTransformer(name)

    def _load_llm_model(self, name: str) -> Any:
        try:
            from transformers import AutoModelForCausalLM, AutoTokenizer
        except ImportError as exc:
            raise RuntimeError(
                "transformers is required for llm backends other than 'simulated'."
            ) from exc

        tokenizer = AutoTokenizer.from_pretrained(name)
        model = AutoModelForCausalLM.from_pretrained(name)

        try:
            import torch

            if torch.cuda.is_available():
                model = model.to("cuda")
        except ImportError:
            pass

        return {"tokenizer": tokenizer, "model": model}


def cosine_similarity(left: List[float], right: List[float]) -> float:
    if len(left) != len(right) or not left:
        return 0.0

    dot = sum(l * r for l, r in zip(left, right))
    left_norm = math.sqrt(sum(value * value for value in left))
    right_norm = math.sqrt(sum(value * value for value in right))
    if left_norm == 0.0 or right_norm == 0.0:
        return 0.0
    return dot / (left_norm * right_norm)


def simulated_embedding(text: str) -> List[float]:
    values = [0.0] * 16
    for index, char in enumerate(text.encode("utf-8")):
        values[index % 16] += float(char) / 255.0
    return values
