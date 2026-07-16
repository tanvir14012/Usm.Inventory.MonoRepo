$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $PSScriptRoot
$certDir = Join-Path $projectRoot "certs"
$certFile = Join-Path $certDir "localhost.pem"
$keyFile = Join-Path $certDir "localhost-key.pem"

if (-not (Get-Command mkcert -ErrorAction SilentlyContinue)) {
  Write-Error "mkcert is not installed or not available in PATH. Install mkcert first: https://github.com/FiloSottile/mkcert"
}

if (-not (Test-Path $certDir)) {
  New-Item -ItemType Directory -Path $certDir | Out-Null
}

Write-Host "Installing local CA (mkcert -install)..."
mkcert -install

Write-Host "Generating localhost certificate..."
mkcert -cert-file $certFile -key-file $keyFile localhost 127.0.0.1 ::1

Write-Host "Certificate created:"
Write-Host "  Cert: $certFile"
Write-Host "  Key:  $keyFile"
