<#
.SYNOPSIS
    Configura os GitHub Secrets necessários para CI/CD.

.DESCRIPTION
    Cria todos os secrets no repositório GitHub via GitHub CLI.
    Execute este script uma única vez antes do primeiro deploy.

    Secrets configurados:
      Fase 11 (Deploy API + Web): SSH_HOST, SSH_PORT, SSH_USER, SSH_KEY,
                                  DB_CONNECTION_STRING, JWT_SECRET, HMAC_SECRET,
                                  CLOUDFLARE_ZONE_ID, CLOUDFLARE_API_TOKEN
      Fase 13 (Android APK):      ANDROID_KEYSTORE_BASE64, ANDROID_KEY_ALIAS,
                                  ANDROID_KEY_PASSWORD, ANDROID_STORE_PASSWORD

.NOTES
    Pré-requisitos:
    - GitHub CLI (gh) autenticado: gh auth login
    - Chave SSH em: C:\Dev\ssh-key-2026-01-17.key
    - Para Android: keystore gerada com keytool (ver seção Android abaixo)

.EXAMPLE
    # Configurar todos os secrets de uma vez:
    .\scripts\setup-github-secrets.ps1

    # Configurar apenas os secrets Android (após gerar a keystore):
    .\scripts\setup-github-secrets.ps1 -ApenasAndroid
#>
param(
    [switch]$ApenasAndroid
)

$repo = "carloscampos2014/LicenciamentoSoftware"

Write-Host "`n[>>] Configurando GitHub Secrets para $repo`n" -ForegroundColor Cyan

if ($ApenasAndroid) {
    Write-Host "[>>] Modo: apenas secrets Android`n" -ForegroundColor Yellow
}

# ── SSH ───────────────────────────────────────────────────────────────────────
if (-not $ApenasAndroid) {
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

    # ── Banco de dados ────────────────────────────────────────────────────────
    Write-Host "[5/7] DB_CONNECTION_STRING..." -ForegroundColor Yellow
    $npgsqlConn  = "Host=db.mnxqgrrkjgelxintdcxf.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=493`$Z9LghYLcvzV;SSL Mode=Require;Trust Server Certificate=true"
    gh secret set DB_CONNECTION_STRING --body $npgsqlConn --repo $repo

    # ── Segredos de aplicação ─────────────────────────────────────────────────
    Write-Host "[6/7] JWT_SECRET..." -ForegroundColor Yellow
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
}

# ── Android (Fase 13) ─────────────────────────────────────────────────────────
Write-Host "`n[>>] Secrets Android (Fase 13 — Build APK)" -ForegroundColor Cyan

$keystorePath = Read-Host "Caminho para o arquivo .keystore (Enter para pular)"

if ($keystorePath -and (Test-Path $keystorePath)) {
    Write-Host "[A1/4] ANDROID_KEYSTORE_BASE64..." -ForegroundColor Yellow
    $keystoreBase64 = [Convert]::ToBase64String([System.IO.File]::ReadAllBytes($keystorePath))
    gh secret set ANDROID_KEYSTORE_BASE64 --body $keystoreBase64 --repo $repo
    Write-Host "     Keystore codificada em Base64 e configurada." -ForegroundColor Gray

    Write-Host "[A2/4] ANDROID_KEY_ALIAS..." -ForegroundColor Yellow
    $keyAlias = Read-Host "Alias da chave na keystore (ex: licensemanager)"
    gh secret set ANDROID_KEY_ALIAS --body $keyAlias --repo $repo

    Write-Host "[A3/4] ANDROID_KEY_PASSWORD..." -ForegroundColor Yellow
    $keyPassword = Read-Host "Senha da chave (key password)" -AsSecureString
    $keyPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($keyPassword))
    gh secret set ANDROID_KEY_PASSWORD --body $keyPasswordPlain --repo $repo

    Write-Host "[A4/4] ANDROID_STORE_PASSWORD..." -ForegroundColor Yellow
    $storePassword = Read-Host "Senha da keystore (store password)" -AsSecureString
    $storePasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($storePassword))
    gh secret set ANDROID_STORE_PASSWORD --body $storePasswordPlain --repo $repo

    Write-Host "`n[OK] Secrets Android configurados!" -ForegroundColor Green
} else {
    Write-Host "[!!] Keystore não fornecida. Secrets Android não configurados." -ForegroundColor Red
    Write-Host ""
    Write-Host "Para gerar uma nova keystore, execute:" -ForegroundColor Cyan
    Write-Host '  keytool -genkey -v -keystore licensemanager.keystore \'
    Write-Host '          -alias licensemanager \'
    Write-Host '          -keyalg RSA -keysize 2048 \'
    Write-Host '          -validity 10000'
    Write-Host ""
    Write-Host "Depois rode novamente com -ApenasAndroid:" -ForegroundColor Cyan
    Write-Host "  .\scripts\setup-github-secrets.ps1 -ApenasAndroid"
}

# ── Resumo ────────────────────────────────────────────────────────────────────
Write-Host "`n[OK] Configuração concluída!" -ForegroundColor Green
Write-Host ""

if (-not $ApenasAndroid) {
    Write-Host "Próximos passos (Deploy API/Web):" -ForegroundColor Cyan
    Write-Host "  1. Copie os valores de JWT_SECRET e HMAC_SECRET acima"
    Write-Host "  2. Atualize /etc/licenciamento/env na VM com esses valores"
    Write-Host "  3. Configure os registros DNS no Cloudflare:"
    Write-Host "     A  licensemanager.enzojb.com.br     → 137.131.209.235"
    Write-Host "     A  licensemanager-api.enzojb.com.br → 137.131.209.235"
    Write-Host "  4. Rode o setup-vm.sh na Oracle VM"
    Write-Host "  5. Faça push para master para disparar o deploy"
    Write-Host ""
}

Write-Host "Próximos passos (Android APK):" -ForegroundColor Cyan
Write-Host "  1. Faça push para master (ou dispare o workflow manualmente)"
Write-Host "  2. Baixe o APK em Actions → Build APK (Android) → Artifacts"
Write-Host "  3. Siga as instruções em docs/instalacao-android.md"
Write-Host ""
