param(
    [string]$OutputDir = ".\certs\cac",
    [string]$Country = "US",
    [string]$State = "State",
    [string]$Locality = "City",
    [string]$Organization = "USM Inventory",
    [string]$OrganizationalUnit = "PKI",
    [string]$CommonName = "USM CAC Root CA",
    [int]$ValidityDays = 3650
)

$openssl = Get-Command openssl -ErrorAction SilentlyContinue
if (-not $openssl) {
    throw "OpenSSL is required but was not found in PATH."
}

$fullOutputDir = Join-Path (Get-Location).Path $OutputDir
New-Item -ItemType Directory -Path $fullOutputDir -Force | Out-Null

$keyPath = Join-Path $fullOutputDir "cac-root-ca.key"
$certPath = Join-Path $fullOutputDir "cac-root-ca.crt"
$configPath = Join-Path $fullOutputDir "cac-root-ca.cnf"

@"
[req]
distinguished_name = dn
x509_extensions = v3_ca
prompt = no

[dn]
C  = $Country
ST = $State
L  = $Locality
O  = $Organization
OU = $OrganizationalUnit
CN = $CommonName

[v3_ca]
basicConstraints = critical,CA:TRUE,pathlen:1
keyUsage = critical,keyCertSign,cRLSign,digitalSignature
subjectKeyIdentifier = hash
authorityKeyIdentifier = keyid:always,issuer
"@ | Set-Content -Path $configPath -Encoding utf8

& openssl genrsa -out $keyPath 4096
if (-not $?) { throw "Failed to generate CA private key." }

& openssl req -x509 -new -nodes `
    -key $keyPath `
    -sha256 `
    -days $ValidityDays `
    -out $certPath `
    -config $configPath `
    -extensions v3_ca
if (-not $?) { throw "Failed to generate CA certificate." }

& openssl x509 -in $certPath -noout -text | Out-Null
if (-not $?) { throw "Generated certificate failed OpenSSL validation." }

Remove-Item $configPath -Force

Write-Host ""
Write-Host "CAC root certificate generated:"
Write-Host "  $certPath"
Write-Host ""
Write-Host "Private key generated:"
Write-Host "  $keyPath"
Write-Host ""
Write-Host "Next steps:"
Write-Host "  1. Trust '$certPath' on client machines used for CAC certificate authentication."
Write-Host "  2. Configure Identity service CAC trust settings to include this root certificate."
Write-Host "  3. Keep '$keyPath' offline and protected."
