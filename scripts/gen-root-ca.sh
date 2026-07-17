#!/bin/sh
# gen-root-ca.sh — Generate Root CA and server TLS certificates for USM Inventory
#
# Usage:
#   ./scripts/gen-root-ca.sh
#
# Environment overrides:
#   CERTS_DIR   Output directory (default: ./certs/ssl)
#   DOMAIN      Server hostname  (default: usm.inventory.local)
#   COUNTRY     Two-letter country code (default: US)
#   ORG         Organisation name (default: USM Inventory)
#
# After running this script, add certs/ssl/ca.crt to your OS/browser trusted
# certificate store.  The docker-compose stack mounts it into every container
# so backend services automatically trust it at startup.

set -eu

CERTS_DIR="${CERTS_DIR:-./certs/ssl}"
DOMAIN="${DOMAIN:-usm.inventory.local}"
COUNTRY="${COUNTRY:-US}"
ORG="${ORG:-USM Inventory}"
DAYS_CA=3650       # 10 years for root CA
DAYS_SERVER=825    # ~2 years for server cert (Apple/browser limit)

mkdir -p "$CERTS_DIR"

# ── Root CA ────────────────────────────────────────────────────────────────────
echo "==> Generating Root CA private key (4096-bit RSA)…"
openssl genrsa -out "$CERTS_DIR/ca.key" 4096

echo "==> Generating Root CA self-signed certificate…"
openssl req -new -x509 \
    -days "$DAYS_CA" \
    -key  "$CERTS_DIR/ca.key" \
    -out  "$CERTS_DIR/ca.crt" \
    -subj "/C=${COUNTRY}/ST=State/L=City/O=${ORG} CA/OU=Infrastructure/CN=${ORG} Root CA"

# ── Server certificate ─────────────────────────────────────────────────────────
echo "==> Generating server private key (2048-bit RSA)…"
openssl genrsa -out "$CERTS_DIR/server.key" 2048

echo "==> Generating server CSR…"
openssl req -new \
    -key  "$CERTS_DIR/server.key" \
    -out  "$CERTS_DIR/server.csr" \
    -subj "/C=${COUNTRY}/ST=State/L=City/O=${ORG}/OU=Web/CN=${DOMAIN}"

echo "==> Writing SAN extension config…"
cat > "$CERTS_DIR/server.ext" <<EOF
[req]
req_extensions     = v3_req
distinguished_name = req_distinguished_name

[req_distinguished_name]

[v3_req]
basicConstraints = CA:FALSE
keyUsage         = digitalSignature, keyEncipherment
extendedKeyUsage = serverAuth
subjectAltName   = @alt_names

[alt_names]
DNS.1 = ${DOMAIN}
DNS.2 = localhost
DNS.3 = *.${DOMAIN}
IP.1  = 127.0.0.1
EOF

echo "==> Signing server certificate with Root CA…"
openssl x509 -req \
    -days "$DAYS_SERVER" \
    -in   "$CERTS_DIR/server.csr" \
    -CA   "$CERTS_DIR/ca.crt" \
    -CAkey "$CERTS_DIR/ca.key" \
    -CAcreateserial \
    -out  "$CERTS_DIR/server.crt" \
    -extfile "$CERTS_DIR/server.ext" \
    -extensions v3_req

# ── Verify chain ───────────────────────────────────────────────────────────────
echo "==> Verifying certificate chain…"
openssl verify -CAfile "$CERTS_DIR/ca.crt" "$CERTS_DIR/server.crt"

# Clean up temporary files
rm -f "$CERTS_DIR/server.csr" "$CERTS_DIR/server.ext" "$CERTS_DIR/ca.srl"

# Set restrictive permissions on private keys
chmod 600 "$CERTS_DIR/ca.key" "$CERTS_DIR/server.key"
chmod 644 "$CERTS_DIR/ca.crt" "$CERTS_DIR/server.crt"

echo ""
echo "✓ Certificates generated in: $CERTS_DIR"
echo ""
echo "  ca.crt     — Root CA certificate  ← add to OS/browser trust store"
echo "  ca.key     — Root CA private key  ← KEEP SECRET"
echo "  server.crt — Server TLS certificate"
echo "  server.key — Server TLS private key  ← KEEP SECRET"
echo ""
echo "Next steps:"
echo "  1. Run 'docker compose up --build'"
echo "  2. Trust certs/ssl/ca.crt in your OS/browser"
