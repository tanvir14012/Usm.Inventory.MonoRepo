from __future__ import annotations

import json
import sys
from dataclasses import dataclass
from typing import Any

_MODEL_CACHE: dict[str, Any] = {}


def _load_model(model: str, kind: str) -> Any:
    key = f"{kind}:{model}"
    cached = _MODEL_CACHE.get(key)
    if cached is not None:
        return cached
    if kind == "embedding":
        cached = [0.01, 0.02, 0.03, 0.04]
    elif kind == "ner":
        cached = {"PERSON": ["Alice"], "ORG": ["Contoso"]}
    elif kind == "classification":
        cached = {"label": "positive", "score": 0.99}
    else:
        cached = None
    _MODEL_CACHE[key] = cached
    return cached


def _handle(request: dict[str, Any]) -> dict[str, Any]:
    request_id = str(request.get("requestId", ""))
    operation = str(request.get("operation", ""))
    model = str(request.get("model", "default"))
    parameters = request.get("parameters") or {}

    if operation == "heartbeat":
        return {"requestId": request_id, "success": True, "result": {"ready": True}}

    if operation == "shutdown":
        return {"requestId": request_id, "success": True, "result": {"shutdown": True}}

    if operation == "embedding":
        _load_model(model, "embedding")
        return {"requestId": request_id, "success": True, "result": _load_model(model, "embedding")}

    if operation == "ner":
        _load_model(model, "ner")
        return {"requestId": request_id, "success": True, "result": _load_model(model, "ner")}

    if operation == "classification":
        _load_model(model, "classification")
        return {"requestId": request_id, "success": True, "result": _load_model(model, "classification")}

    if operation == "summarization":
        return {"requestId": request_id, "success": True, "result": "summary"}

    if operation == "invoke":
        return {"requestId": request_id, "success": True, "result": {"arguments": parameters.get("arguments", {})}}

    return {"requestId": request_id, "success": False, "error": {"code": "unknown", "message": operation}}


def main() -> None:
    for raw in sys.stdin:
        line = raw.strip()
        if not line:
            continue
        request = json.loads(line)
        response = _handle(request)
        sys.stdout.write(json.dumps(response, separators=(",", ":")) + "\n")
        sys.stdout.flush()


if __name__ == "__main__":
    main()
