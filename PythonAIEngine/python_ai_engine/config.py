from __future__ import annotations

import json
import os
from dataclasses import dataclass, field
from typing import Any, Dict, List


@dataclass(slots=True)
class ModelWarmup:
    name: str
    kind: str = "embedding"


@dataclass(slots=True)
class EngineConfig:
    host: str = "0.0.0.0"
    port: int = 50051
    max_concurrent_rpcs: int = 256
    warmup_models: bool = True
    warmups: List[ModelWarmup] = field(default_factory=list)
    allow_arbitrary_code: bool = True
    default_llm_backend: str = "simulated"
    default_embedding_backend: str = "simulated"


def load_config() -> EngineConfig:
    raw = os.getenv("AI_ENGINE_CONFIG_JSON")
    if raw:
        payload = json.loads(raw)
        return EngineConfig(
            host=payload.get("host", "0.0.0.0"),
            port=int(payload.get("port", 50051)),
            max_concurrent_rpcs=int(payload.get("max_concurrent_rpcs", 256)),
            warmup_models=bool(payload.get("warmup_models", True)),
            warmups=[ModelWarmup(**item) for item in payload.get("warmups", [])],
            allow_arbitrary_code=bool(payload.get("allow_arbitrary_code", True)),
            default_llm_backend=payload.get("default_llm_backend", "simulated"),
            default_embedding_backend=payload.get("default_embedding_backend", "simulated"),
        )

    return EngineConfig()


def to_json(value: Any) -> str:
    return json.dumps(value, ensure_ascii=False, separators=(",", ":"))

