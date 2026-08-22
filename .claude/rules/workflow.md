---
description: "Workflow de branches, commits e PRs — ciclo completo de feature/fase."
---

# Workflow de Desenvolvimento

## Ciclo Completo

```
master atualizado
      ↓
  criar branch  →  publicar branch
      ↓
  briefing  →  aguardar aprovação
      ↓
  implementar (loop: commit → build → testes)
      ↓
  atualizar documentação
      ↓
  push  →  criar PR (com Closes #N)
      ↓
  aguardar merge  →  limpar branches locais
```

## Regras de Branch

```bash
git fetch origin
git checkout master
git pull origin master
git checkout -b feature/<nome-descritivo>
git push -u origin feature/<nome-descritivo>
```

- Padrão: `feature/fase-X-descricao` ou `feature/nome-da-feature`
- Nunca implementar diretamente no `master`
- Publicar branch antes do primeiro commit de código

## Briefing Obrigatório

Antes de escrever qualquer código em tarefas não triviais:
1. Lista de arquivos que serão criados/modificados
2. O que cada parte faz
3. Decisões de design relevantes
4. O que NÃO será feito (escopo negativo)

Aguardar "aprovado", "pode implementar" ou equivalente antes de prosseguir.

## Commits (Conventional Commits)

```
<tipo>(<escopo>): #N descrição concisa
```

Tipos: `feat`, `fix`, `docs`, `refactor`, `tests`, `chore`, `perf`

## Pull Request

```bash
gh pr create \
  --base master \
  --head feature/<nome> \
  --title "feat: descrição concisa (máx 70 chars)" \
  --body "..."
```

Body deve incluir: resumo, mudanças por área, resultado dos testes, `Closes #N`.

## Após Merge

```bash
git checkout master && git pull origin master
git remote prune origin
git branch -D feature/<nome>
```
