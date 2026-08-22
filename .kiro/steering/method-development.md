---
inclusion: manual
description: "Loop estruturado de 7 passos para implementação — aplique sempre que implementar feature, corrigir bug ou refatorar"
---

# Method: Desenvolvimento

Loop de 7 passos para implementação. Aplique sempre que: implementar feature nova,
corrigir bug, refatorar, ou reescrever código legado.

Os passos estruturam seu trabalho — não narre os números de etapa ao usuário.

---

## Gate de Trivialidade (executar primeiro)

Um pedido é trivial APENAS se TODAS forem verdade:
- Um arquivo, menos de ~10 linhas alteradas
- Sem comportamento novo (rename, typo, ajuste de config)
- O que mudar é óbvio sem pesquisa

Se trivial: fazer a mudança, confirmar com o check óbvio, reportar em 2 frases.
Tudo mais recebe o loop completo.

---

## Step 0 — Classificar o pedido

| Forma | Sinal | Entregável |
|-------|-------|-----------|
| **Bug fix** | "corrigir", "não funciona", "erro em" | Correção verificada + twin check |
| **Feature** | "implementar", "criar", "adicionar" | Implementação completa, testada |
| **Refactoring** | "melhorar", "limpar", "extrair" | Código refatorado, testes verdes |
| **Plan-first** | escopo ambíguo, ações irreversíveis, pedido de plano | Plano com recomendação — parar e aguardar aprovação |

Se escopo ambíguo e evidência pode resolver: prossiga.
Se só o usuário pode resolver: fazer UMA pergunta específica com interpretação recomendada.

---

## Step 1 — Definir "done"

Em 1-2 frases, o que "pronto" significa e como será verificado.

Declare assunções load-bearing. Se uma é verificável com 1 busca: verificar em vez de assumir.

---

## Step 2 — Coletar evidência

### 2.1 Orientar antes de codar
- Listar estrutura do projeto (camadas, módulos principais)
- Ler spec/requirements se existir (`.kiro/specs/`)
- Verificar padrões existentes no código vizinho
- Verificar `.kiro/knowledge/` — pode já ter documentação do módulo

### 2.2 Fontes primárias vencem memória
- Ler o código existente antes de estender
- Para APIs/libs: buscar docs atuais
- Nunca inventar assinatura, endpoint ou payload de memória

### 2.3 Paralelizar o que é independente
Ler spec + código existente + testes relacionados em um único batch.

### 2.4 Ler estreito
- Grep para localizar implementações similares
- `read_code` com selector para métodos específicos
- Não ler arquivos inteiros sem necessidade

### 2.5 Time-box
- 2 rodadas de lookup cobrem a maioria das tasks
- 3ª rodada precisa de razão declarada
- Se 2 lookups consecutivos não trouxeram nada novo: pare

### 2.6 Surpresas re-roteiam
Contradição com expectativa = achado mais importante.
Declarar ao usuário, atualizar Step 1 se muda o "done".

---

## Step 3 — Decidir e comprometer

Sintetizar em **uma recomendação**. Se considerou alternativas, nomear cada em 1 linha
dizendo por que perdeu.

- Task reversível + escopo claro: prosseguir para Step 4 sem pedir permissão
- Plan-first (escopo ambíguo, irreversível): apresentar plano e PARAR

---

## Step 4 — Implementar

### 4.1 Intent Gate — declarar antes de mudar comportamento
Antes de alterar comportamento existente:
```
INTENT: alterando [X] de [comportamento atual] para [comportamento novo]
porque [razão de 1 frase]
```

### 4.2 Recall Gate — antes de usar qualquer API/lib
"Tenho certeza de que esta assinatura/endpoint existe?"
Se não: buscar no código ou docs antes de usar.

### 4.3 Menor mudança que satisfaz o pedido
Não refatorar, reformatar ou "melhorar" fora do escopo declarado.

### 4.4 Seguir o padrão do código vizinho
Não introduzir novo estilo ou biblioteca sem mencionar ao usuário.

### 4.5 Checklist de implementação
Para cada item do pedido original, verificar: implementado? testado? documentado?
Nenhum item pode cair silenciosamente.

### 4.6 Proibições permanentes (sem instrução explícita)
- Nunca commit ou push
- Nunca enfraquecer teste ou fabricar o que ele verifica
- Nunca tocar secrets/credentials/env
- Nunca adicionar dependência sem mencionar
- Nunca deletar/sobrescrever fora do escopo declarado

---

## Step 5 — Verificar por observação

### 5a — Critério de done observado (não inferido de ler o código)
- Build: rodar o comando de build do projeto
- Testes: rodar o comando de teste do projeto
- Output mostrado ao usuário, não "deve funcionar"

### 5b — Sistema circundante saudável
- Build não quebrou
- Testes existentes não falharam
- Lint OK se aplicável

### 5c — Twin Check (quando corrigiu defeito)
Buscar o mesmo padrão errado no projeto inteiro. Reportar:
```
TWINS: busquei <padrão> — encontrei <N> outros locais: <arquivos, ou "nenhum">
```

---

## Step 6 — Reportar outcome-first

- Primeira frase: o que aconteceu
- Evidência: output de build/testes
- Caveats: o que foi pulado, o que não pôde verificar

### Linhas obrigatórias (quando aplicável)
- `INTENT:` quando mudou comportamento
- `PENDING: <ação> — aguardando autorização` quando follow-up não foi tomado
- `TWINS:` quando corrigiu defeito

---

## Failure Modes

| # | Falha | Prevenido por |
|---|-------|---------------|
| 1 | Fix não pedido | Step 0: pergunta → só findings |
| 2 | API inventada | Step 2.2 + Step 4.2 Recall Gate |
| 3 | Scope creep | Step 4.3 menor mudança |
| 4 | Teste enfraquecido | Step 4.6 proibição |
| 5 | Retry thrash | Step 5 hard bound de 3 ciclos |
| 6 | Verification theater | Step 5a observado, não inferido |
| 7 | Ação não autorizada | Step 3 plan-first gate |
| 8 | Surpresa ignorada | Step 2.6 surpresas re-roteiam |
| 9 | Item dropado silenciosamente | Step 4.5 checklist obrigatória |
| 10 | Twin perdido | Step 5c Twin Check |
