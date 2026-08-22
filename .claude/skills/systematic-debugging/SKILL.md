---
name: systematic-debugging
description: Diagnóstico estruturado de bugs — da hipótese à correção verificada com Twin Check. Ativar com /debug ou mencionando "investigar bug", "erro estranho".
---

# Skill: Systematic Debugging

## Etapa 1 — Definir o problema com precisão

```
❌ "O sistema está lento"
✅ "A listagem demora >5s em produção com >1000 registros,
    mas é rápida em dev com <100. Começou após deploy v2.3."
```

Responder:
- O que acontece? (comportamento observado)
- O que deveria acontecer?
- Quando acontece? Quando NÃO acontece?
- Desde quando?

## Etapa 2 — Reproduzir de forma isolada

```
✅ Criar teste que reproduz o bug
✅ Isolar variáveis (remover dependências externas)
❌ "Corrigir" sem conseguir reproduzir
```

## Etapa 3 — Formular hipóteses

```
Hipótese 1: [causa mais provável] — evidência: [dado]
Hipótese 2: [causa alternativa]   — evidência: [dado]
```

Priorizar por: mudanças recentes, padrão do erro, stack trace disponível.

## Etapa 4 — Testar uma hipótese por vez

```
Hipótese 1: [método de teste] → [confirmou/refutou] — [evidência]
```

**Hard bound:** após 3 hipóteses refutadas sem progresso → parar e reportar ao usuário.

## Etapa 5 — Aplicar correção mínima

```
✅ Menor mudança que corrige a causa raiz
✅ Manter teste que reproduzia o bug
❌ Workaround que esconde o sintoma
❌ catch { } para suprimir o erro
```

## Etapa 6 — Twin Check

```
TWINS: busquei <padrão> — encontrei <N> locais: <arquivos ou "nenhum">
```

## Debugging por tipo

| Tipo | Primeira ação |
|------|--------------|
| Lógica (resultado errado) | Adicionar logs nos inputs/outputs, rastrear de fora para dentro |
| Concorrência (race condition) | Verificar recursos compartilhados, transações, async/await |
| Performance (lento) | Medir por seção, checar N+1, verificar índices |
| Intermitente | Aumentar logging, verificar hora/data, buscar o que é diferente |

## Formato do relatório

```
🔍 Diagnóstico — [título]
Causa raiz: [descrição]
Correção: [arquivo:linha] — [o que mudou]
Teste adicionado: [NomeDoTeste]
Build: ✅ | Testes: ✅ [N] passando
TWINS: [resultado da busca]
```
