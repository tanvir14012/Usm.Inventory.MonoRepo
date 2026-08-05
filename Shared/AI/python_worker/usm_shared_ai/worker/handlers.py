from __future__ import annotations

import asyncio
from abc import ABC, abstractmethod
from typing import Any, Dict, List

from .config import as_serializable
from .protocol import Request


class OperationHandler(ABC):
    operations: set[str]

    @abstractmethod
    async def handle(self, request: Request, state) -> Any:
        raise NotImplementedError


class EmbeddingHandler(OperationHandler):
    operations = {"embedding", "embeddings"}

    async def handle(self, request: Request, state) -> Any:
        model = request.model or "sentence-transformers/all-MiniLM-L6-v2"
        transformer = await state.get_embedding_model(model)

        if request.operation == "embeddings":
            texts = request.parameters.get("texts") or [request.parameters.get("text", "")]
            if not isinstance(texts, list):
                texts = [str(texts)]
            vector = transformer.encode(texts, convert_to_numpy=True, normalize_embeddings=True)
            return as_serializable(vector)

        text = str(request.parameters.get("text", ""))
        vector = transformer.encode(text, convert_to_numpy=True, normalize_embeddings=True)
        return as_serializable(vector)


class TransformersHandler(OperationHandler):
    operations = {"classification", "sentiment", "summarization", "translation"}

    async def handle(self, request: Request, state) -> Any:
        model = request.model or default_model_for_operation(request.operation)
        task = task_for_operation(request.operation)
        pipeline = await state.get_pipeline(task, model)

        if request.operation == "translation":
            text = str(request.parameters.get("text", ""))
            src = request.parameters.get("source_language")
            tgt = request.parameters.get("target_language")
            result = pipeline(text, src_lang=src, tgt_lang=tgt) if src or tgt else pipeline(text)
            return as_serializable(result)

        if request.operation == "summarization":
            text = str(request.parameters.get("text", ""))
            result = pipeline(
                text,
                max_length=int(request.parameters.get("max_length", 130)),
                min_length=int(request.parameters.get("min_length", 30)),
                do_sample=False,
            )
            return as_serializable(result)

        text = str(request.parameters.get("text", ""))
        result = pipeline(text)
        return as_serializable(result)


class SpacyHandler(OperationHandler):
    operations = {"ner"}

    async def handle(self, request: Request, state) -> Any:
        model = request.model or "en_core_web_sm"
        nlp = await state.get_spacy_model(model)
        doc = nlp(str(request.parameters.get("text", "")))
        entities: Dict[str, List[str]] = {}

        for ent in doc.ents:
            entities.setdefault(ent.label_, []).append(ent.text)

        return entities


class CustomFunctionHandler(OperationHandler):
    operations = {"invoke"}

    async def handle(self, request: Request, state) -> Any:
        operation = str(request.parameters.get("function") or request.parameters.get("name") or "")
        if not operation:
            raise ValueError("Custom invocation requires a function name.")

        function = await state.get_custom_function(operation)
        args = request.parameters.get("arguments") or request.parameters.get("args") or {}
        if asyncio.iscoroutinefunction(function):
            return await function(args)
        return function(args)


def default_model_for_operation(operation: str) -> str:
    if operation == "translation":
        return "Helsinki-NLP/opus-mt-en-ROMANCE"
    if operation == "summarization":
        return "facebook/bart-large-cnn"
    return "distilbert-base-uncased-finetuned-sst-2-english"


def task_for_operation(operation: str) -> str:
    return {
        "classification": "text-classification",
        "sentiment": "sentiment-analysis",
        "summarization": "summarization",
        "translation": "translation",
    }[operation]

