FROM python:3.11-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /app

COPY Shared/AI/python_worker/pyproject.toml /app/python_worker/pyproject.toml
COPY Shared/AI/python_worker/usm_shared_ai /app/python_worker/usm_shared_ai
COPY Shared/AI/python_worker/test_fixtures /app/python_worker/test_fixtures

RUN pip install --no-cache-dir /app/python_worker[all]

WORKDIR /app/python_worker

USER 10001:10001

ENTRYPOINT ["python", "-m", "usm_shared_ai.worker"]
