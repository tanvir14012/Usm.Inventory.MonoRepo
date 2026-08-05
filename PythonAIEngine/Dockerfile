FROM python:3.11-slim AS builder

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /build

RUN apt-get update \
    && apt-get install -y --no-install-recommends build-essential gcc \
    && rm -rf /var/lib/apt/lists/*

COPY Protos/ai_engine.proto /build/Protos/ai_engine.proto
COPY PythonAIEngine/pyproject.toml /build/PythonAIEngine/pyproject.toml
COPY PythonAIEngine/python_ai_engine /build/PythonAIEngine/python_ai_engine

RUN pip install --no-cache-dir grpcio-tools \
    && mkdir -p /build/PythonAIEngine/python_ai_engine/generated \
    && python -m grpc_tools.protoc \
      -I /build/Protos \
      --python_out=/build/PythonAIEngine/python_ai_engine/generated \
      --grpc_python_out=/build/PythonAIEngine/python_ai_engine/generated \
      /build/Protos/ai_engine.proto

FROM python:3.11-slim AS runtime

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    NVIDIA_VISIBLE_DEVICES=all \
    NVIDIA_DRIVER_CAPABILITIES=compute,utility

WORKDIR /app

COPY --from=builder /build/PythonAIEngine /app

RUN pip install --no-cache-dir grpcio grpcio-health-checking protobuf numpy \
    && pip install --no-cache-dir --extra-index-url https://download.pytorch.org/whl/cu124 torch torchvision torchaudio \
    && pip install --no-cache-dir .[ml]

EXPOSE 50051

USER 10001:10001

ENTRYPOINT ["python", "-m", "python_ai_engine"]

