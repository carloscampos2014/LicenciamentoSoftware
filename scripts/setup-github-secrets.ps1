<#
.SYNOPSIS
    Configura os GitHub Secrets necessários para CI/CD da Fase 11.

.DESCRIPTION
    Cria todos os secrets no repositório GitHub via GitHub CLI.
    Execute este script uma única vez antes do primeiro deploy.

.NOTES
    Pré-requisitos:
    - GitHub CLI (gh) autenticado: gh auth login
    - Chave SSH em: C:\Dev\ssh-key-2026-01-17.key
    - Ter os valores de JWT_SECRET e HMAC_SECRET prontos (gerar abaixo)

.EXAMPLE
    .\scripts\setup-github-secrets.ps1
#>

$repo = "carloscampos2014/LicenciamentoSoftware"

Write-Host "`n[>>] Configurando GitHub Secrets para $repo`n" -ForegroundColor Cyan

# ── SSH ───────────────────────────────────────────────────────────────────────
Write-Host "[1/7] SSH_HOST..." -ForegroundColor Yellow
gh secret set SSH_HOST --body "137.131.209.235" --repo $repo

Write-Host "[2/7] SSH_PORT..." -ForegroundColor Yellow
gh secret set SSH_PORT --body "22022" --repo $repo

Write-Host "[3/7] SSH_USER..." -ForegroundColor Yellow
gh secret set SSH_USER --body "ubuntu" --repo $repo

Write-Host "[4/7] SSH_KEY..." -ForegroundColor Yellow
$sshKeyPath = "C:\Dev\ssh-key-2026-01-17.key"
if (Test-Path $sshKeyPath) {
    $sshKeyContent = Get-Content $sshKeyPath -Raw
    gh secret set SSH_KEY --body $sshKeyContent --repo $repo
    Write-Host "     Chave SSH carregada de $sshKeyPath" -ForegroundColor Gray
} else {
    Write-Host "[!!] Chave SSH não encontrada em $sshKeyPath" -ForegroundColor Red
    Write-Host "     Ajuste o caminho e rode novamente." -ForegroundColor Red
}

# ── Banco de dados ────────────────────────────────────────────────────────────
Write-Host "[5/7] DB_CONNECTION_STRING..." -ForegroundColor Yellow
# Converte a URL do Supabase para formato Npgsql
# postgresql://postgres:PASS@host:5432/postgres → Host=host;Port=5432;Database=postgres;Username=postgres;Password=PASS
$supabaseUrl = "postgresql://postgres:493`$Z9LghYLcvzV@db.mnxqgrrkjgelxintdcxf.supabase.co:5432/postgres"
$npgsqlConn  = "Host=db.mnxqgrrkjgelxintdcxf.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=493`$Z9LghYLcvzV;SSL Mode=Require;Trust Server Certificate=true"
gh secret set DB_CONNECTION_STRING --body $npgsqlConn --repo $repo

# ── Segredos de aplicação ─────────────────────────────────────────────────────
Write-Host "[6/7] JWT_SECRET..." -ForegroundColor Yellow
# Gera um secret seguro de 64 chars se não fornecido
$jwtSecret = -join ((65..90) + (97..122) + (48..57) + (33, 35, 36, 38, 42, 43, 45, 61, 63, 64) |
    Get-Random -Count 64 | ForEach-Object { [char]$_ })
Write-Host "     JWT_SECRET gerado: $jwtSecret" -ForegroundColor Gray
Write-Host "     ⚠️  Guarde este valor com segurança!" -ForegroundColor Yellow
gh secret set JWT_SECRET --body $jwtSecret --repo $repo

Write-Host "[7/7] HMAC_SECRET..." -ForegroundColor Yellow
$hmacSecret = -join ((65..90) + (97..122) + (48..57) |
    Get-Random -Count 48 | ForEach-Object { [char]$_ })
Write-Host "     HMAC_SECRET gerado: $hmacSecret" -ForegroundColor Gray
Write-Host "     ⚠️  Guarde este valor com segurança!" -ForegroundColor Yellow
gh secret set HMAC_SECRET --body $hmacSecret --repo $repo

# ── Resumo ────────────────────────────────────────────────────────────────────
Write-Host "`n[OK] Todos os secrets configurados!" -ForegroundColor Green
Write-Host ""
Write-Host "Próximos passos:" -ForegroundColor Cyan
Write-Host "  1. Copie os valores de JWT_SECRET e HMAC_SECRET acima"
Write-Host "  2. Atualize /etc/licenciamento/env na VM com esses valores"
Write-Host "  3. Configure os registros DNS no Cloudflare:"
Write-Host "     A  licensemanager.enzojb.com.br     → 137.131.209.235"
Write-Host "     A  api.licensemanager.enzojb.com.br → 137.131.209.235"
Write-Host "  4. Rode o setup-vm.sh na Oracle VM"
Write-Host "  5. Faça push para master para disparar o deploy"
Write-Host ""
