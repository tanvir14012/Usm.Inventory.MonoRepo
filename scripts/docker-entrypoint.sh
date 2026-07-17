#!/bin/sh
# docker-entrypoint.sh — Shared entrypoint for all .NET backend services on Alpine.
#
# Installs the USM Root CA certificate (mounted via docker-compose volume) into
# the Alpine trust store so that inter-service HTTPS calls are trusted.
# Falls back cleanly if the cert file is not present.

set -e

CA_CERT="/usr/local/share/ca-certificates/usm-root-ca.crt"

if [ -f "$CA_CERT" ]; then
    echo "[entrypoint] Installing USM Root CA certificate…"
    update-ca-certificates --fresh 2>/dev/null || true
fi

exec "$@"
