<#
.SYNOPSIS
    Inicia o ambiente local usando Docker para o PostgreSQL.

.DESCRIPTION
    1. Verifica pre-requisitos (Docker, .NET SDK)
    2. Cria o arquivo .env se nao existir
    3. Sobe o PostgreSQL via docker compose
    4. Aguarda o banco ficar pronto
    5. Inicia a API (migrations DbUp aplicadas automaticamente na startup)

.PARAMETER Stop
    Para e remove os containers sem iniciar a API.

.PARAMETER Rebuild
    Recria os containers (util para resetar o banco).

.EXAMPLE
    .\scripts\start-docker.ps1
    .\scripts\start-docker.ps1 -Stop
    .\scripts\start-docker.ps1 -Rebuild

.NOTES
    Requer: Docker Desktop (ou Docker Engine no WSL2) e .NET 10 SDK
#>
param(
    [switch]$Stop,
    [switch]$Rebuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Write-Step { param($msg) Write-Host "[>>] $msg" -ForegroundColor Cyan    }
function Write-Ok   { param($msg) Write-Host "[OK] $msg" -ForegroundColor Green   }
function Write-Fail { param($msg) Write-Host "[!!] $msg" -ForegroundColor Red; exit 1 }
function Write-Info { param($msg) Write-Host "     $msg" -ForegroundColor Gray    }
function Write-Warn { param($msg) Write-Host "[AV] $msg" -ForegroundColor Yellow  }

$root = Split-Path $PSScriptRoot -Parent

# --------------------------------------------------------------------------
# Pre-requisitos
# --------------------------------------------------------------------------
Write-Step "Verificando pre-requisitos..."

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
    Write-Fail "Docker nao encontrado. Instale o Docker Desktop: https://www.docker.com/products/docker-desktop"
}
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Fail ".NET SDK nao encontrado. Instale em: https://dotnet.microsoft.com/download/dotnet/10.0"
}

$dotnetVersion = (dotnet --version)
if (-not $dotnetVersion.StartsWith("10.")) {
    Write-Warn "SDK encontrado e '$dotnetVersion'. O projeto requer .NET 10."
}

Write-Ok "Docker e .NET $dotnetVersion encontrados."

# --------------------------------------------------------------------------
# .env
# --------------------------------------------------------------------------
$envFile = Join-Path $root ".env"

if (-not (Test-Path $envFile)) {
    Write-Step "Criando .env a partir de .env.example..."
    Copy-Item (Join-Path $root ".env.example") $envFile
    Write-Host ""
    Write-Warn "Arquivo .env criado com senha padrao."
    Write-Info "Edite '$envFile' e defina DB_PASSWORD antes de continuar."
    Write-Host ""
    $resposta = Read-Host "  Continuar com a senha padrao? (s/N)"
    if ($resposta -notin @("s", "S", "sim", "Sim")) {
        Write-Info "Abra o arquivo .env, defina DB_PASSWORD e execute o script novamente."
        exit 0
    }
}

Write-Ok ".env configurado."

# --------------------------------------------------------------------------
# Stop
# --------------------------------------------------------------------------
if ($Stop) {
    Write-Step "Parando containers..."
    docker compose -f (Join-Path $root "docker-compose.yml") down
    Write-Ok "Containers parados."
    exit 0
}

# --------------------------------------------------------------------------
# Rebuild
# --------------------------------------------------------------------------
if ($Rebuild) {
    Write-Step "Removendo containers e volumes existentes..."
    docker compose -f (Join-Path $root "docker-compose.yml") down -v
    Write-Ok "Ambiente limpo."
}

# --------------------------------------------------------------------------
# Subir PostgreSQL
# --------------------------------------------------------------------------
Write-Step "Subindo PostgreSQL via docker compose..."
docker compose -f (Join-Path $root "docker-compose.yml") up -d

# --------------------------------------------------------------------------
# Aguardar banco
# --------------------------------------------------------------------------
Write-Step "Aguardando PostgreSQL ficar pronto..."
$maxTentativas = 30
$tentativa     = 0
$pronto        = $false

while ($tentativa -lt $maxTentativas) {
    docker compose -f (Join-Path $root "docker-compose.yml") exec -T postgres `
        pg_isready -U licenciamento -d licenciamento_dev 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) { $pronto = $true; break }
    $tentativa++
    Write-Info "Tentativa $tentativa/$maxTentativas -- aguardando..."
    Start-Sleep -Seconds 2
}

if (-not $pronto) {
    Write-Fail "PostgreSQL nao ficou pronto. Verifique: docker compose logs postgres"
}

Write-Ok "PostgreSQL pronto."

# --------------------------------------------------------------------------
# JWT Secret
# --------------------------------------------------------------------------
if (-not $env:JwtSettings__Secret) {
    $env:JwtSettings__Secret = "desenvolvimento-local-chave-secreta-minimo-32-chars!"
    Write-Info "JWT secret de desenvolvimento definido para esta sessao."
}

# --------------------------------------------------------------------------
# Iniciar API
# --------------------------------------------------------------------------
Write-Step "Iniciando a API..."
Write-Host ""
Write-Host "+----------------------------------------------------------+" -ForegroundColor Cyan
Write-Host "|  API:          http://localhost:5016                     |" -ForegroundColor Cyan
Write-Host "|  Health:       http://localhost:5016/health              |" -ForegroundColor Cyan
Write-Host "|  OpenAPI JSON: http://localhost:5016/openapi/v1.json     |" -ForegroundColor Cyan
Write-Host "|  Banco:        licenciamento_dev @ localhost:5432        |" -ForegroundColor Cyan
Write-Host "|                                                          |" -ForegroundColor Cyan
Write-Host "|  Ctrl+C para parar                                       |" -ForegroundColor Cyan
Write-Host "+----------------------------------------------------------+" -ForegroundColor Cyan
Write-Host ""
Write-Info "Dica: importe http://localhost:5016/openapi/v1.json no Postman ou Insomnia."
Write-Host ""

dotnet run --project (Join-Path $root "src\LicenciamentoSoftware.Api\LicenciamentoSoftware.Api.csproj") `
    --launch-profile "http"
