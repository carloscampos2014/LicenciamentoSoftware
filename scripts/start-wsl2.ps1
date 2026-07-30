<#
.SYNOPSIS
    Inicia o ambiente local usando o PostgreSQL já rodando no WSL2.

.DESCRIPTION
    1. Verifica pré-requisitos (.NET SDK)
    2. Detecta o IP do WSL2 (ou usa localhost)
    3. Solicita (ou reutiliza) as credenciais do banco
    4. Testa a conectividade com o PostgreSQL
    5. Inicia a API (migrations DbUp aplicadas automaticamente na startup)

.PARAMETER DbHost
    Host do PostgreSQL. Padrão: localhost (funciona na maioria dos casos com WSL2).

.PARAMETER DbPort
    Porta do PostgreSQL. Padrão: 5432.

.PARAMETER DbName
    Nome do banco. Padrão: licenciamento_dev.

.PARAMETER DbUser
    Usuário do PostgreSQL. Padrão: postgres.

.PARAMETER DbPassword
    Senha do PostgreSQL. Se não informado, será solicitado interativamente.

.PARAMETER JwtSecret
    Secret para o JWT. Se não informado, usa um valor de desenvolvimento.

.EXAMPLE
    .\scripts\start-wsl2.ps1
    .\scripts\start-wsl2.ps1 -DbPassword "minha_senha"
    .\scripts\start-wsl2.ps1 -DbHost "172.20.0.1" -DbUser "licenciamento" -DbPassword "senha"

.NOTES
    Pré-requisito no WSL2:
        sudo service postgresql start
        sudo -u postgres createdb licenciamento_dev   # se o banco ainda não existir
#>
param(
    [string]$DbHost     = "localhost",
    [string]$DbPort     = "5432",
    [string]$DbName     = "licenciamento_dev",
    [string]$DbUser     = "postgres",
    [string]$DbPassword = "",
    [string]$JwtSecret  = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ── Cores ──────────────────────────────────────────────────────────────────────
function Write-Step { param($msg) Write-Host "▶ $msg" -ForegroundColor Cyan }
function Write-Ok   { param($msg) Write-Host "✔ $msg" -ForegroundColor Green }
function Write-Fail { param($msg) Write-Host "✘ $msg" -ForegroundColor Red; exit 1 }
function Write-Info { param($msg) Write-Host "  $msg" -ForegroundColor Gray }
function Write-Warn { param($msg) Write-Host "  ⚠ $msg" -ForegroundColor Yellow }

$root = Split-Path $PSScriptRoot -Parent

# ── Pré-requisitos ─────────────────────────────────────────────────────────────
Write-Step "Verificando pré-requisitos..."

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Fail ".NET SDK não encontrado. Instale em: https://dotnet.microsoft.com/download/dotnet/10.0"
}

$dotnetVersion = (dotnet --version)
if (-not $dotnetVersion.StartsWith("10.")) {
    Write-Warn "SDK encontrado é '$dotnetVersion'. O projeto requer .NET 10."
}

Write-Ok ".NET $dotnetVersion encontrado."

# ── Detecção de IP do WSL2 ─────────────────────────────────────────────────────
if ($DbHost -eq "localhost") {
    Write-Step "Detectando IP do WSL2..."
    try {
        $wslIp = (wsl hostname -I 2>$null).Trim().Split(" ")[0]
        if ($wslIp -match "^\d+\.\d+\.\d+\.\d+$") {
            Write-Info "IP do WSL2 detectado: $wslIp"
            Write-Info "Usando 'localhost' — funciona quando o PostgreSQL está no WSL2 com port forwarding."
            Write-Info "Se a conexão falhar, tente: .\start-wsl2.ps1 -DbHost '$wslIp'"
        }
    } catch {
        Write-Info "WSL2 não detectado — usando 'localhost'."
    }
}

# ── Credenciais ────────────────────────────────────────────────────────────────
if ([string]::IsNullOrWhiteSpace($DbPassword)) {
    Write-Step "Informe a senha do PostgreSQL..."
    Write-Info "Usuário: $DbUser | Banco: $DbName | Host: ${DbHost}:${DbPort}"
    $securePass = Read-Host "  Senha" -AsSecureString
    $DbPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePass))
}

# ── Montar connection string ───────────────────────────────────────────────────
$connectionString = "Host=$DbHost;Port=$DbPort;Database=$DbName;Username=$DbUser;Password=$DbPassword"

# ── Testar conectividade ───────────────────────────────────────────────────────
Write-Step "Testando conectividade com o PostgreSQL..."

# Testa via psql no WSL2 se disponível, senão tenta via .NET direto na startup
$psqlDisponivel = $false
try {
    $testeWsl = wsl which psql 2>$null
    if ($testeWsl -match "psql") { $psqlDisponivel = $true }
} catch { }

if ($psqlDisponivel) {
    $testeConexao = wsl psql "postgresql://${DbUser}:${DbPassword}@${DbHost}:${DbPort}/${DbName}" `
        -c "SELECT 1" 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Warn "Não foi possível conectar ao PostgreSQL. Verifique:"
        Write-Info "  1. O PostgreSQL está rodando no WSL2?"
        Write-Info "     Execute no WSL: sudo service postgresql start"
        Write-Info "  2. O banco '$DbName' existe?"
        Write-Info "     Execute no WSL: sudo -u postgres createdb $DbName"
        Write-Info "  3. As credenciais estão corretas?"
        Write-Info "     Usuário: $DbUser | Host: $DbHost | Porta: $DbPort"
        Write-Host ""
        $continuar = Read-Host "  Tentar iniciar a API mesmo assim? (s/N)"
        if ($continuar -notin @("s", "S", "sim", "Sim")) { exit 1 }
    } else {
        Write-Ok "PostgreSQL respondendo em ${DbHost}:${DbPort}."
    }
} else {
    Write-Info "psql não disponível — a conexão será validada na inicialização da API."
}

# ── Configurar variáveis de ambiente ──────────────────────────────────────────
Write-Step "Configurando variáveis de ambiente..."

$env:ConnectionStrings__DefaultConnection = $connectionString
$env:ASPNETCORE_ENVIRONMENT = "Development"

if ([string]::IsNullOrWhiteSpace($JwtSecret)) {
    $env:JwtSettings__Secret = "desenvolvimento-local-chave-secreta-minimo-32-chars!"
    Write-Info "JWT secret de desenvolvimento definido para esta sessão."
} else {
    $env:JwtSettings__Secret = $JwtSecret
}

Write-Ok "Variáveis configuradas."

# ── Iniciar API ────────────────────────────────────────────────────────────────
Write-Step "Iniciando a API (migrations DbUp aplicadas automaticamente)..."
Write-Host ""
Write-Host "  ┌──────────────────────────────────────────────────────┐" -ForegroundColor Cyan
Write-Host "  │  API:              http://localhost:5016              │" -ForegroundColor Cyan
Write-Host "  │  Health:           http://localhost:5016/health       │" -ForegroundColor Cyan
Write-Host "  │  OpenAPI JSON:     http://localhost:5016/openapi/v1.json │" -ForegroundColor Cyan
Write-Host "  │                                                       │" -ForegroundColor Cyan
Write-Host "  │  Banco:   $DbName @ ${DbHost}:${DbPort}$('' * [Math]::Max(0, 18 - $DbName.Length - $DbHost.Length))│" -ForegroundColor Cyan
Write-Host "  │                                                       │" -ForegroundColor Cyan
Write-Host "  │  Pressione Ctrl+C para parar a API                    │" -ForegroundColor Cyan
Write-Host "  └──────────────────────────────────────────────────────┘" -ForegroundColor Cyan
Write-Host ""
Write-Info "Dica: importe http://localhost:5016/openapi/v1.json no Postman ou Insomnia."
Write-Host ""

dotnet run --project (Join-Path $root "src\LicenciamentoSoftware.Api\LicenciamentoSoftware.Api.csproj") `
    --launch-profile "http"
