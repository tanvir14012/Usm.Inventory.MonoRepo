from __future__ import annotations

import base64
import json
import os
from dataclasses import asdict

from .protocol import BootstrapConfig, CustomFunctionConfig, ModelConfig


def load_bootstrap_config() -> BootstrapConfig:
    config_file = os.getenv("USM_SHARED_AI_CONFIG_FILE")
    if config_file:
        with open(config_file, "r", encoding="utf-8") as handle:
            payload = json.load(handle)
        return BootstrapConfig(
            worker_id=_read(payload, "worker_id", "workerId"),
            pool_name=_read(payload, "pool_name", "poolName"),
            role=_read(payload, "role", "role"),
            protocol_version=int(_read(payload, "protocol_version", "protocolVersion")),
            minimum_python_version=_read(payload, "minimum_python_version", "minimumPythonVersion"),
            warmup_models=bool(_read(payload, "warmup_models", "warmupModels")),
            models=[ModelConfig(**_normalize_model(item)) for item in payload.get("models", [])],
            custom_functions=[CustomFunctionConfig(**_normalize_custom_function(item)) for item in payload.get("custom_functions", payload.get("customFunctions", []))],
        )

    raw = os.getenv("USM_SHARED_AI_CONFIG")
    if not raw:
        raise RuntimeError("USM_SHARED_AI_CONFIG or USM_SHARED_AI_CONFIG_FILE is required.")

    decoded = base64.b64decode(raw.encode("utf-8")).decode("utf-8")
    payload = json.loads(decoded)
    return BootstrapConfig(
        worker_id=_read(payload, "worker_id", "workerId"),
        pool_name=_read(payload, "pool_name", "poolName"),
        role=_read(payload, "role", "role"),
        protocol_version=int(_read(payload, "protocol_version", "protocolVersion")),
        minimum_python_version=_read(payload, "minimum_python_version", "minimumPythonVersion"),
        warmup_models=bool(_read(payload, "warmup_models", "warmupModels")),
        models=[ModelConfig(**_normalize_model(item)) for item in payload.get("models", [])],
        custom_functions=[CustomFunctionConfig(**_normalize_custom_function(item)) for item in payload.get("custom_functions", payload.get("customFunctions", []))],
    )


def as_serializable(value):
    if hasattr(value, "tolist"):
        return value.tolist()
    if isinstance(value, dict):
        return {k: as_serializable(v) for k, v in value.items()}
    if isinstance(value, (list, tuple)):
        return [as_serializable(v) for v in value]
    if hasattr(value, "__dict__"):
        return asdict(value)
    return value


def _read(payload, *keys):
    for key in keys:
        if key in payload:
            return payload[key]
    raise KeyError(keys[0])


def _normalize_model(item):
    return {
        "name": item.get("name") or item.get("Name"),
        "role": item.get("role") or item.get("Role"),
    }


def _normalize_custom_function(item):
    return {
        "operation": item.get("operation") or item.get("Operation"),
        "module": item.get("module") or item.get("Module"),
        "function": item.get("function") or item.get("Function"),
        "model": item.get("model") or item.get("Model"),
    }
