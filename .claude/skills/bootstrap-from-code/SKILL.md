---
name: bootstrap-from-code
description: Analisa o codigo-fonte de um projeto existente e gera o comando bootstrap.ps1 completo, detectando stack, estrutura e comandos de build. Ativar com /bootstrap-from-code ou "gerar bootstrap a partir do codigo".
tools: Read, Bash, Glob
model: sonnet
---

# Skill: Bootstrap from Code

Analisa o código-fonte de um projeto que ainda não tem harness configurado e monta o comando `bootstrap.ps1` mais adequado.

## Como ativar

`/bootstrap-from-code` ou dizer "gera o bootstrap baseado no código" / "lê o projeto e monta o bootstrap"

---

## Processo

### Etapa 1 — Detectar stack

Ler arquivos de configuração na raiz:
- `*.sln`, `*.csproj` → .NET (C#)
- `package.json` → Node.js/TypeScript
- `pom.xml`, `build.gradle` → Java/Kotlin
- `Cargo.toml` → Rust
- `go.mod` → Go
- `requirements.txt`, `pyproject.toml` → Python
- `pubspec.yaml` → Flutter/Dart

Ler também `README.md`, `Makefile`, `.github/workflows/*.yml` — revelam comandos exatos.

### Etapa 2 — Inferir comandos

Com base na stack:
- `.NET` → `dotnet build *.sln -c Debug` / `dotnet test tests/`
- `Node/npm` → `npm run build` / `npm test`
- `Python` → `python -m build` / `pytest`
- `Go` → `go build ./...` / `go test ./...`

Preferir o comando exato do CI/README quando disponível.

### Etapa 3 — Detectar repositório

Extrair owner/repo de `git remote get-url origin`.

### Etapa 4 — Detectar ferramentas já presentes

Verificar `.kiro/`, `.claude/`, `.github/`, `.amazonq/`, `.trae/`, `AGENTS.md`, `GEMINI.md`.
Avisar quais já existem — bootstrap não sobrescreve arquivos existentes.

### Etapa 5 — Apresentar resultado

```
## Bootstrap gerado a partir do código

### Stack detectada
[stack com evidências — qual arquivo revelou cada coisa]

### Parâmetros identificados
| Parâmetro | Valor | Fonte |
|-----------|-------|-------|
| ...

### Ferramentas recomendadas
[quais flags usar e por quê]

### Parâmetros pendentes
[o que não foi possível detectar]

### Comando
\`\`\`powershell
& "C:\Dev\agentfiles\scripts\bootstrap.ps1" `
    -ProjectPath "..." `
    ...
\`\`\`
```

---

## Regras

- Nunca executar o bootstrap diretamente
- Sempre indicar a fonte de cada parâmetro detectado
- Se o projeto já tiver harness parcial, sugerir `update-harness.ps1` em vez do bootstrap
