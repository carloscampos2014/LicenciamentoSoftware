# install-git-hooks.ps1
# Instala os git hooks locais do projeto.
# Execute uma vez apos clonar o repositorio:
#   .\scripts\install-git-hooks.ps1

$rootDir   = Split-Path -Parent $PSScriptRoot
$hooksDir  = Join-Path $rootDir ".git\hooks"
$sourceDir = Join-Path $PSScriptRoot "git-hooks"

if (-not (Test-Path $hooksDir)) {
    Write-Error "Pasta .git\hooks nao encontrada. Certifique-se de estar na raiz do repositorio."
    exit 1
}

$hooks = @("pre-commit", "commit-msg", "pre-push")

foreach ($hook in $hooks) {
    $source      = Join-Path $sourceDir $hook
    $destination = Join-Path $hooksDir  $hook

    if (-not (Test-Path $source)) {
        Write-Warning "Hook '$hook' nao encontrado em $sourceDir. Pulando."
        continue
    }

    Copy-Item -Path $source -Destination $destination -Force

    # Garantir permissao de execucao no WSL/Git Bash
    $gitBash = "C:\Program Files\Git\bin\bash.exe"
    if (Test-Path $gitBash) {
        $unixPath = ($destination -replace "\\", "/") -replace "^C:", "/mnt/c"
        & $gitBash -c "chmod +x '$unixPath'"
    }

    Write-Host "OK Hook instalado: $hook" -ForegroundColor Green
}

Write-Host ""
Write-Host "Git hooks instalados com sucesso!" -ForegroundColor Cyan
Write-Host ""
Write-Host "Hooks ativos:"
Write-Host "  pre-commit  - build + testes unitarios + deteccao de segredos"
Write-Host "  commit-msg  - valida Conventional Commits"
Write-Host "  pre-push    - build completo + todos os testes + protecao de main"
