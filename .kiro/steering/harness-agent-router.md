---
inclusion: always
description: "Classifica pedidos do usuário e carrega apenas o contexto necessário — evita poluir o contexto com steerings irrelevantes"
---

# Agent Router — Classificação de Pedidos

Antes de responder qualquer pedido não trivial, classifique-o e carregue apenas
o contexto mínimo necessário. Não ative todos os steerings "por precaução".

---

## Tabela de Classificação

| Tipo de pedido | Sinais | Contexto a carregar |
|----------------|--------|---------------------|
| **Feature nova** | "implementar", "criar", "adicionar" | engineering-standards, project-standards, method-development |
| **Bug fix** | "corrigir", "não funciona", "erro em" | method-development, engineering-standards |
| **Refactoring** | "melhorar", "limpar", "extrair", "refatorar" | engineering-standards, method-development |
| **Code review** | "revisar", "analisar", "verificar código" | engineering-standards |
| **Spec / planejamento** | "planejar", "especificar", "desenhar" | workflow-aprovacao, workflow-desenvolvimento |
| **Pergunta técnica** | "como", "por que", "explica" | apenas contexto direto |
| **Git / PR** | "commit", "branch", "PR", "merge" | git-commits, workflow-desenvolvimento |
| **Retomar trabalho** | "continuar", "o que falta", "retomar" | continuar-de-onde-paramos (manual) |
| **Documentação** | "documentar", "atualizar docs" | project-standards |

---

## Regras

1. **Um steering por vez** quando possível — não empilhar contexto desnecessário.
2. **Steerings `manual`** só são carregados quando o usuário digita `#nome` explicitamente.
3. Se o pedido for ambíguo, fazer UMA pergunta de classificação antes de prosseguir.
4. Pedidos triviais (1 arquivo, < 10 linhas, sem lógica nova) → responder diretamente sem roteamento.

---

## Critério de Trivialidade

Um pedido é trivial APENAS se TODAS forem verdade:
- Um arquivo, menos de ~10 linhas alteradas
- Sem comportamento novo (rename, typo, ajuste de config)
- O que mudar é óbvio sem pesquisa

Se trivial: fazer a mudança, confirmar com build/lint/teste, reportar em 2 frases.
Tudo mais recebe o loop completo de desenvolvimento.
