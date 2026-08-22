---
inclusion: auto
description: "Padrão de commits — Conventional Commits, granularidade por task, regras de segurança"
---

# Padrão de Commits

## Formato (Conventional Commits)

```
<tipo>(<escopo>): #N descrição concisa em imperativo

[corpo opcional — o que e por que, não como]
[Closes #N para fechar issue automaticamente no merge]
```

## Tipos

| Tipo | Quando usar |
|------|-------------|
| `feat` | Nova funcionalidade |
| `fix` | Correção de bug |
| `docs` | Documentação apenas |
| `refactor` | Refactoring sem mudança de comportamento |
| `tests` | Adição ou ajuste de testes |
| `chore` | Configuração, dependências, infraestrutura |
| `perf` | Melhoria de performance |

## Regras

- Mensagem em **imperativo** ("adicionar", "corrigir", não "adicionado", "corrigido")
- Máximo 72 caracteres na primeira linha
- Incluir `#N` quando o commit avança ou fecha uma issue
- Um commit por issue/tarefa coesa — não aglomerar mudanças não relacionadas
- Build e testes devem passar antes de commitar

## Sequência segura

```powershell
# 1. Verificar o que será commitado
git diff --staged

# 2. Build e testes
<comando de build do projeto>
<comando de testes do projeto>

# 3. Commitar apenas arquivos relevantes (nunca git add .)
git add <arquivos-especificos>
git commit -m "tipo(escopo): #N descrição"
```

## Proibições

- Nunca `git add .` — staged acidentalmente arquivos não relacionados
- Nunca commitar com build quebrado
- Nunca commitar secrets, .env, ou arquivos de credenciais
- Nunca usar `--no-verify` salvo instrução explícita do usuário
- Nunca `--amend` em commits já publicados no remoto
- Nunca `push --force` salvo instrução explícita do usuário
