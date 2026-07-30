<#
.SYNOPSIS
    Inicia o ambiente local usando o PostgreSQL ja rodando no WSL2.

.DESCRIPTION
    1. Verifica pre-requisitos (.NET SDK)
    2. Detecta o IP do WSL2 (ou usa localhost)
    3. Solicita (ou reutiliza) as credenciais do banco
    4. Testa a conectividade com o PostgreSQL
    5. Inicia a API (migrations DbUp aplicadas automaticamente na startup)

.PARAMETER DbHost
    Host do PostgreSQL. Padrao: localhost (funciona na maioria dos casos com WSL2).

.PARAMETER DbPort
    Porta do PostgreSQL. Padrao: 5432.

.PARAMETER DbName
    Nome do banco. Padrao: licenciamento_dev.

.PARAMETER DbUser
    Usuario do PostgreSQL. Padrao: postgres.

.PARAMETER DbPassword
    Senha do PostgreSQL. Se nao informado, sera solicitado interativamente.

.PARAMETER JwtSecret
    Secret para o JWT. Se nao informado, usa um valor de desenvolvimento.

.EXAMPLE
    .\scripts\start-wsl2.ps1
    .\scripts\start-wsl2.ps1 -DbPassword "minha_senha"
    .\scripts\start-wsl2.ps1 -DbHost "172.20.0.1" -DbUser "licenciamento" -DbPassword "senha"
    .\scripts\start-wsl2.ps1 -DbName "licenciamento" -DbPassword "senha"

.NOTES
    Pre-requisito no WSL2:
        sudo service postgresql start
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

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Fail ".NET SDK nao encontrado. Instale em: https://dotnet.microsoft.com/download/dotnet/10.0"
}

$dotnetVersion = (dotnet --version)
if (-not $dotnetVersion.StartsWith("10.")) {
    Write-Warn "SDK encontrado e '$dotnetVersion'. O projeto requer .NET 10."
}

Write-Ok ".NET $dotnetVersion encontrado."

# --------------------------------------------------------------------------
# Detectar IP do WSL2
# --------------------------------------------------------------------------
if ($DbHost -eq "localhost") {
    Write-Step "Detectando IP do WSL2..."
    try {
        $wslIp = (wsl hostname -I 2>$null).Trim().Split(" ")[0]
        if ($wslIp -match "^\d+\.\d+\.\d+\.\d+$") {
            Write-Info "IP do WSL2 detectado: $wslIp"
            Write-Info "Usando 'localhost' -- funciona quando o PostgreSQL esta no WSL2 com port forwarding."
            Write-Info "Se a conexao falhar, tente: .\start-wsl2.ps1 -DbHost '$wslIp'"
        }
    } catch {
        Write-Info "WSL2 nao detectado -- usando 'localhost'."
    }
}

# --------------------------------------------------------------------------
# Credenciais
# --------------------------------------------------------------------------
if ([string]::IsNullOrWhiteSpace($DbPassword)) {
    Write-Step "Informe a senha do PostgreSQL..."
    Write-Info "Usuario: $DbUser | Banco: $DbName | Host: ${DbHost}:${DbPort}"
    $securePass = Read-Host "  Senha" -AsSecureString
    $DbPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePass))
}

$connectionString = "Host=$DbHost;Port=$DbPort;Database=$DbName;Username=$DbUser;Password=$DbPassword"

# --------------------------------------------------------------------------
# Testar conectividade
# --------------------------------------------------------------------------
Write-Step "Testando conectividade com o PostgreSQL..."

$psqlDisponivel = $false
try {
    $testeWsl = wsl which psql 2>$null
    if ($testeWsl -match "psql") { $psqlDisponivel = $true }
} catch { }

if ($psqlDisponivel) {
    wsl psql "postgresql://${DbUser}:${DbPassword}@${DbHost}:${DbPort}/${DbName}" -c "SELECT 1" 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Warn "Nao foi possivel conectar ao PostgreSQL. Verifique:"
        Write-Info "  1. O PostgreSQL esta rodando no WSL2?"
        Write-Info "     Execute no WSL: sudo service postgresql start"
        Write-Info "  2. O banco '$DbName' sera criado automaticamente pelo DbUp na startup."
        Write-Info "     Mas o usuario '$DbUser' precisa ter permissao CREATEDB."
        Write-Info "     Execute no WSL: sudo -u postgres psql -c ""ALTER USER $DbUser CREATEDB;"""
        Write-Info "  3. As credenciais estao corretas?"
        Write-Info "     Usuario: $DbUser | Host: $DbHost | Porta: $DbPort"
        Write-Host ""
        $continuar = Read-Host "  Tentar iniciar a API mesmo assim? (s/N)"
        if ($continuar -notin @("s", "S", "sim", "Sim")) { exit 1 }
    } else {
        Write-Ok "PostgreSQL respondendo em ${DbHost}:${DbPort}."
    }
} else {
    Write-Info "psql nao disponivel -- a conexao sera validada na inicializacao da API."
}

# --------------------------------------------------------------------------
# Variaveis de ambiente
# --------------------------------------------------------------------------
Write-Step "Configurando variaveis de ambiente..."

$env:ConnectionStrings__DefaultConnection = $connectionString
$env:ASPNETCORE_ENVIRONMENT = "Development"

if ([string]::IsNullOrWhiteSpace($JwtSecret)) {
    $env:JwtSettings__Secret = "desenvolvimento-local-chave-secreta-minimo-32-chars!"
    Write-Info "JWT secret de desenvolvimento definido para esta sessao."
} else {
    $env:JwtSettings__Secret = $JwtSecret
}

Write-Ok "Variaveis configuradas."

# --------------------------------------------------------------------------
# Iniciar API
# --------------------------------------------------------------------------
Write-Step "Iniciando a API..."
Write-Host ""
Write-Host "+----------------------------------------------------------+" -ForegroundColor Cyan
Write-Host "|  API:          http://localhost:5016                     |" -ForegroundColor Cyan
Write-Host "|  Health:       http://localhost:5016/health              |" -ForegroundColor Cyan
Write-Host "|  OpenAPI JSON: http://localhost:5016/openapi/v1.json     |" -ForegroundColor Cyan
Write-Host "|  Banco:        $DbName @ ${DbHost}:${DbPort}" -ForegroundColor Cyan
Write-Host "|                                                          |" -ForegroundColor Cyan
Write-Host "|  Ctrl+C para parar                                       |" -ForegroundColor Cyan
Write-Host "+----------------------------------------------------------+" -ForegroundColor Cyan
Write-Host ""
Write-Info "Dica: importe http://localhost:5016/openapi/v1.json no Postman ou Insomnia."
Write-Host ""

dotnet run --project (Join-Path $root "src\LicenciamentoSoftware.Api\LicenciamentoSoftware.Api.csproj") `
    --launch-profile "http"
