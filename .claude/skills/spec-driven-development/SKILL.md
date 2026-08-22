---
name: spec-driven-development
description: Desenvolvimento guiado por especificação — requirements → design → tasks → implementação. Ativar com /spec ou mencionando "criar spec", "especificar feature".
---

# Skill: Spec-Driven Development

## Quando criar spec

| Situação | Criar? |
|----------|--------|
| Feature com 3+ classes/componentes | ✅ Sim |
| Trabalho > 4 horas | ✅ Sim |
| Mudança de contrato de API ou banco | ✅ Sim |
| Qualquer mudança irreversível | ✅ Sim |
| Bugfix simples (1-2 arquivos) | ❌ Não |

## Estrutura

```
.kiro/specs/<nome>/
├── requirements.md
├── design.md
└── tasks.md
```

## Template requirements.md

```markdown
# Requirements — [Feature]

## Contexto
[Por que existe. Qual problema resolve.]

## User Stories

### US-01: [Título]
**Como** [usuário] **Quero** [ação] **Para** [valor]

**Critérios de Aceite:**
- CA-01: [Dado/Quando/Então]

**Fora do escopo:** [o que esta story NÃO faz]

## Regras de Negócio
| ID | Regra |
|----|-------|
| RN-01 | [regra objetiva] |
```

## Template design.md

```markdown
# Design — [Feature]

## Componentes afetados
- [Componente] — [o que muda]

## Fluxo principal
[diagrama mermaid sequenceDiagram]

## Decisões de Design
| Decisão | Alternativa | Motivo |
|---------|------------|--------|
```

## Template tasks.md

```markdown
# Tasks — [Feature]

- [ ] **Task 1.1** — [descrição]
  - Arquivos: [lista]
  - Critérios: [CA-01], [CA-02]
  - Estimativa: P/M/G
```

## Processo

```
Elicitar requisitos → requirements.md → aprovação
→ design.md → aprovação
→ tasks.md
→ implementar task por task (build + testes em cada)
→ validar CAs ao final
```

**Regra:** Nunca implementar sem requirements aprovados.
