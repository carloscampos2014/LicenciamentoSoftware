# Skill: Bootstrap from Docs

Lê documentos de ideia, requisitos ou briefing e monta o comando `bootstrap.ps1` completo.

## Como ativar

`/bootstrap-from-docs` ou "gera o bootstrap baseado nesses documentos"

O usuário deve indicar onde estão os documentos, onde está o agentfiles e onde será criado o projeto.

---

## Processo

### Etapa 1 — Ler os documentos

Procurar por:
- **Nome do projeto** — título do documento, nome da pasta
- **Descrição** — objetivo em 1-2 frases
- **Stack** — linguagens, frameworks, bancos, serviços externos
- **Plataforma** — web, mobile, desktop, API
- **GitHub owner/repo** — se mencionados
- **Build/test commands** — se referenciados

### Etapa 2 — Inferir o que não está explícito

- Mobile → MAUI/.NET ou React Native
- Integração Twilio → .NET ou Node
- "API REST + banco relacional" → inferir stack adequada

Se não for possível inferir, perguntar antes de gerar.

### Etapa 3 — Detectar ferramentas adequadas

- `-Kiro` — sempre
- `-Claude`, `-Copilot`, `-AmazonQ`, `-Codex`, `-Gemini`, `-Trae` — conforme mencionado pelo usuário

### Etapa 4 — Apresentar resultado

```
## Bootstrap gerado

### Parâmetros identificados
| Parâmetro | Valor | Fonte |
|-----------|-------|-------|
| ProjectName | ... | ... |

### Parâmetros inferidos
| Parâmetro | Valor | Raciocínio |
|-----------|-------|-----------|
| BuildCommand | ... | ... |

### Parâmetros pendentes
- [o que precisa ser preenchido manualmente]

### Comando
\`\`\`powershell
& "C:\Dev\agentfiles\scripts\bootstrap.ps1" `
    -ProjectPath "..." `
    -ProjectName "..." `
    -ProjectDescription "..." `
    -StackDescription "..." `
    -GithubOwner "..." `
    -GithubRepo "..." `
    -BuildCommand "..." `
    -TestCommand "..." `
    -Kiro -Claude
\`\`\`
```

## Regras

- Nunca executar o bootstrap — apenas gerar o comando para o usuário executar
- Se documentos estiverem vagos, listar o que precisa ser definido antes
