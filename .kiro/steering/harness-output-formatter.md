---
inclusion: always
description: "Padrão de output — o que vai no chat vs. em arquivo, formato de respostas, como manter o chat enxuto"
---

# Padrão de Output

## Regra de Ouro

**Chat = decisões, resumos e próximos passos.**
**Arquivos = detalhes, logs, código extenso e análises.**

---

## Formato por Tipo de Resposta

### Implementação de código

```
✅ [Artefato] implementado
Localização: [caminho/do/arquivo]
Decisões principais:
  - [decisão 1]
  - [decisão 2]
Testes: [N] criados — [todos passando / X falhando]
Próximo: [próxima ação]
```

Não exibir o código completo no chat a menos que o usuário peça explicitamente.

### Análise / Code Review

```
📊 Análise de [arquivo/módulo]
Problemas encontrados: [N]
  🔴 Crítico: [N] — [descrição breve]
  🟡 Atenção: [N] — [descrição breve]
  🔵 Info: [N]
Ação recomendada: [próximo passo]
```

### Build / Testes

```
🔨 Build: ✅ OK / ❌ [N] erros
🧪 Testes: ✅ [N] passando / ❌ [N] falhando
[Se erro]: Falha em [Classe.Método] — [mensagem resumida]
```

### Conclusão de task

```
✅ Task [N] concluída
Implementado: [lista em bullets]
Testes: [N] novos, todos passando
Próxima task: [descrição]
```

---

## Tamanho Máximo no Chat

| Tipo | Máximo no chat | Excedente vai para |
|------|---------------|-------------------|
| Análise | 20 linhas | `.kiro/quality/` |
| Código gerado | 10 linhas (trecho principal) | arquivo do projeto |
| Lista de problemas | 5 itens (top críticos) | `.kiro/quality/review-*.md` |

---

## Tabelas em vez de prosa

Sempre preferir tabelas para comparações, listas de itens e status. Nunca re-exibir
informação já apresentada — referenciar o arquivo onde está.
