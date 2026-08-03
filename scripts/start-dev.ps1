# =============================================================================
# start-dev.ps1 - Sobe o ambiente de desenvolvimento completo
#
# O que faz:
#   1. Verifica se o PostgreSQL esta acessivel
#   2. Sobe a API em uma janela de terminal separada
#   3. Sobe o BFF + Blazor WASM em outra janela
#   4. Abre o browser em https://localhost:7152
#
# Uso:
#   .\scripts\start-dev.ps1
#
# Pre-requisitos:
#   - .NET 10 SDK instalado
#   - PostgreSQL rodando (WSL2 ou Docker)
#   - User Secrets configurados na API (ou variaveis de ambiente abaixo)
# =============================================================================

$Root = Split-Path $PSScriptRoot -Parent

# Variaveis de ambiente para dev (ajuste conforme seu ambiente)
# Descomente e preencha se nao usar User Secrets do Visual Studio
#
# $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=licenciamento_dev;Username=postgres;Password=SUA-SENHA"
# $env:JwtSettings__Secret = "sua-chave-secreta-minimo-32-caracteres-aqui"
# $env:HmacSettings__Secret = "sua-chave-hmac-minimo-32-caracteres-aqui"

Write-Host ""
Write-Host "  LicenseManager - Ambiente de Desenvolvimento" -ForegroundColor Cyan
Write-Host "  ==============================================" -ForegroundColor Cyan
Write-Host ""

# [1] Verificar PostgreSQL
Write-Host "[1/3] Verificando PostgreSQL..." -ForegroundColor Yellow

$pgOk = $false
try {
    $conn = New-Object System.Net.Sockets.TcpClient
    $conn.Connect("localhost", 5432)
    $conn.Close()
    $pgOk = $true
    Write-Host "      PostgreSQL: OK (localhost:5432)" -ForegroundColor Green
} catch {
    Write-Host "      PostgreSQL: NAO ENCONTRADO em localhost:5432" -ForegroundColor Red
    Write-Host ""
    Write-Host "      Opcoes para subir o PostgreSQL:" -ForegroundColor Yellow
    Write-Host "        WSL2:   wsl sudo service postgresql start"
    Write-Host "        Docker: .\scripts\start-docker.ps1"
    Write-Host ""
    $resposta = Read-Host "      Continuar mesmo assim? (s/N)"
    if ($resposta -ne "s" -and $resposta -ne "S") {
        Write-Host "      Abortado." -ForegroundColor Red
        exit 1
    }
}

# [2] Subir API
Write-Host ""
Write-Host "[2/3] Iniciando API (http://localhost:5016)..." -ForegroundColor Yellow

$apiDir = Join-Path $Root "src\LicenciamentoSoftware.Api"
$apiCmd = "Set-Location '$apiDir'; Write-Host '  [API] Iniciando...' -ForegroundColor Cyan; dotnet run --launch-profile http"
Start-Process powershell -ArgumentList "-NoExit", "-Command", $apiCmd -WindowStyle Normal

# Aguarda a API iniciar (polling no health check)
Write-Host "      Aguardando API iniciar" -NoNewline -ForegroundColor Gray
$apiOk = $false
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep -Seconds 2
    try {
        $r = Invoke-WebRequest -Uri "http://localhost:5016/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction SilentlyContinue
        if ($r.StatusCode -eq 200) {
            $apiOk = $true
            break
        }
    } catch {
        # ainda nao respondeu
    }
    Write-Host "." -NoNewline -ForegroundColor Gray
}

Write-Host ""
if ($apiOk) {
    Write-Host "      API: OK (http://localhost:5016/health)" -ForegroundColor Green
} else {
    Write-Host "      API ainda nao respondeu - verifique a janela da API" -ForegroundColor Yellow
}

# [3] Subir BFF + Blazor WASM
Write-Host ""
Write-Host "[3/3] Iniciando BFF + Blazor WASM (https://localhost:7152)..." -ForegroundColor Yellow

$webDir = Join-Path $Root "src\LicenciamentoSoftware.Web.Server"
$webCmd = "Set-Location '$webDir'; Write-Host '  [WEB] Iniciando...' -ForegroundColor Cyan; dotnet run --launch-profile https"
Start-Process powershell -ArgumentList "-NoExit", "-Command", $webCmd -WindowStyle Normal

Write-Host "      Aguardando BFF iniciar..." -ForegroundColor Gray
Start-Sleep -Seconds 8

# Abrir browser
Write-Host ""
Write-Host "  Abrindo browser em https://localhost:7152..." -ForegroundColor Cyan
Start-Process "https://localhost:7152"

Write-Host ""
Write-Host "  ======================================" -ForegroundColor Green
Write-Host "  Ambiente iniciado!" -ForegroundColor Green
Write-Host "  ======================================" -ForegroundColor Green
Write-Host ""
Write-Host "  URL principal : https://localhost:7152" -ForegroundColor White
Write-Host "  API           : http://localhost:5016" -ForegroundColor White
Write-Host "  Health check  : http://localhost:5016/health" -ForegroundColor White
Write-Host "  Scalar (docs) : https://localhost:7075/scalar/v1" -ForegroundColor White
Write-Host ""
Write-Host "  Para parar: feche as janelas da API e do BFF" -ForegroundColor Gray
Write-Host ""
