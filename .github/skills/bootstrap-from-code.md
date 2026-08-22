# Skill: Bootstrap from Code

Analisa o código-fonte de um projeto existente e monta o comando `bootstrap.ps1` completo.

## Como ativar

`/bootstrap-from-code` ou "gera o bootstrap baseado no código" / "lê o projeto e monta o bootstrap"

---

## Processo

### Etapa 1 — Detectar stack

Ler arquivos de configuração na raiz:
- `*.sln`, `*.csproj` → .NET (C#)
- `package.json` → Node.js/TypeScript
- `pom.xml`, `build.gradle` → Java/Kotlin
- `Cargo.toml` → Rust | `go.mod` → Go
- `requirements.txt`, `pyproject.toml` → Python
- `pubspec.yaml` → Flutter/Dart

Ler também `README.md`, `Makefile`, `.github/workflows/*.yml`.

### Etapa 2 — Inferir comandos de build/test

- `.NET` → `dotnet build *.sln` / `dotnet test tests/`
- `Node` → `npm run build` / `npm test`
- `Python` → `python -m build` / `pytest`
- `Go` → `go build ./...` / `go test ./...`

Preferir comando exato do CI/README quando disponível.

### Etapa 3 — Detectar repositório

Extrair owner/repo da URL do remote git.

### Etapa 4 — Detectar ferramentas já presentes

Verificar `.kiro/`, `.claude/`, `.github/`, `.amazonq/`, `.trae/`, `AGENTS.md`, `GEMINI.md`.
Avisar sobre as existentes — bootstrap não sobrescreve arquivos existentes.

### Etapa 5 — Apresentar resultado

```
## Bootstrap gerado a partir do código

### Stack detectada
[com evidências — qual arquivo revelou cada coisa]

### Parâmetros identificados
| Parâmetro | Valor | Fonte |
|-----------|-------|-------|

### Parâmetros pendentes
[o que não foi detectado]

### Comando
\`\`\`powershell
& "C:\Dev\agentfiles\scripts\bootstrap.ps1" `
    -ProjectPath "..." `
    ...
\`\`\`
```

## Regras

- Nunca executar o bootstrap diretamente
- Sempre indicar a fonte de cada parâmetro
- Se projeto já tem harness parcial, sugerir `update-harness.ps1` em vez do bootstrap
