---
inclusion: auto
description: "Evidência obrigatória antes de claims de conclusão — nunca afirmar 'concluído' sem output real"
---

# Verification Report — Evidência Antes de Conclusão

## Regra Fundamental

Nunca afirmar que algo está "funcionando", "concluído" ou "ok" sem evidência observada.
Evidência = output real de comando executado, não inferência do código lido.

---

## Checklist Antes de Reportar Conclusão

Antes de dizer "task concluída" ou "implementado com sucesso", verificar:

- [ ] Build passou sem erros? (mostrar as últimas linhas do output)
- [ ] Testes existentes continuam passando? (mostrar contagem)
- [ ] Novos testes foram escritos para nova lógica? (se aplicável)
- [ ] O comportamento pedido foi implementado? (verificar contra o requisito original)
- [ ] Arquivos fora do escopo foram modificados? (se sim, justificar)

---

## Formato do Verification Report

```
## Evidência de Conclusão

**Build:** ✅ OK — 0 erros, 0 warnings
**Testes:** ✅ 42 passando, 0 falhando
**Arquivos modificados:** [lista]
**Comportamento verificado:** [como foi confirmado]

[Se algo não pôde ser verificado]:
**Não verificado:** [o que] — [por que não foi possível verificar]
```

---

## Twin Check (para correções de bug)

Ao corrigir um bug, sempre buscar o mesmo padrão errado no projeto inteiro:

```
TWINS: busquei <padrão> — encontrei <N> outros locais: <arquivos, ou "nenhum">
```

Corrigir todos os locais ou listar explicitamente. Claim de completude sem busca é falha.

---

## Hard Bound

Após 3 ciclos fix-verify na mesma issue sem progresso:
PARAR. Reportar o que foi tentado, o output real e a hipótese atual.
Devolver ao usuário para decisão.
