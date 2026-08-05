from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional


@dataclass(slots=True)
class Request:
    request_id: str
    operation: str
    model: Optional[str]
    parameters: Dict[str, Any] = field(default_factory=dict)
    correlation_id: Optional[str] = None
    worker_role: Optional[str] = None
    stream: Optional[bool] = None
    protocol_version: Optional[int] = None


@dataclass(slots=True)
class Error:
    code: str
    message: str
    details: Optional[str] = None
    stack_trace: Optional[str] = None


@dataclass(slots=True)
class Response:
    request_id: str
    success: bool
    result: Any = None
    error: Optional[Error] = None
    worker_id: Optional[str] = None
    correlation_id: Optional[str] = None
    duration_ms: Optional[int] = None


@dataclass(slots=True)
class ModelConfig:
    name: str
    role: str


@dataclass(slots=True)
class CustomFunctionConfig:
    operation: str
    module: str
    function: str
    model: Optional[str] = None


@dataclass(slots=True)
class BootstrapConfig:
    worker_id: str
    pool_name: str
    role: str
    protocol_version: int
    minimum_python_version: str
    warmup_models: bool
    models: List[ModelConfig] = field(default_factory=list)
    custom_functions: List[CustomFunctionConfig] = field(default_factory=list)

