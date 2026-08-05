---
inclusion: manual
---

# Skill: Continuar de Onde Paramos

Quando o usuário pedir para "continuar de onde paramos", "retomar", "o que falta fazer" ou expressões similares, execute **obrigatoriamente** as etapas abaixo na ordem definida antes de qualquer outra ação.

---

## Etapa 1 — Atualizar repositório local

```powershell
git fetch origin
git checkout master
git pull origin master
```

Confirmar que está no master atualizado antes de continuar.

---

## Etapa 2 — Coletar todas as issues abertas

```powershell
gh issue list --repo carloscampos2014/LicenciamentoSoftware --state open --limit 100 --json number,title,labels,body
```

Coletar também os PRs abertos que possam indicar trabalho em andamento:

```powershell
gh pr list --repo carloscampos2014/LicenciamentoSoftware --state open --json number,title,headRefName
```

---

## Etapa 3 — Analisar o código atual vs issues abertas

Para cada issue aberta, verificar se a implementação **já existe no código atual** usando:

- Busca por palavras-chave do título da issue nos arquivos relevantes
- Verificação das migrations existentes (se a issue envolve banco de dados)
- Verificação dos endpoints, handlers, páginas e testes

Critério de "já implementado":
- O código correspondente existe E está no branch master
- A funcionalidade pode ser verificada no código sem precisar de build

---

## Etapa 4 — Fechar issues já implementadas

Para cada issue identificada como já implementada:

```powershell
gh issue close <numero> --repo carloscampos2014/LicenciamentoSoftware --comment "Implementado. Verificado no código atual do master."
```

---

## Etapa 5 — Montar lista de pendências

Após o levantamento, apresentar ao usuário uma lista organizada com:

**Formato da lista:**

```
## Issues pendentes de implementação

### Fase XX — <Nome da Fase>
- [ ] #<numero> — <titulo> | Prioridade: <alta/média/baixa> | Labels: <labels>

### Sem fase definida
- [ ] #<numero> — <titulo> | Labels: <labels>

### PRs em andamento
- [ ] #<numero> — <titulo> | Branch: <branch>
```

**Critérios de ordenação:**
1. Issues com número de fase no título (Fase 12, Fase 13...) — ordenar pela fase
2. Dentro da mesma fase — ordenar pelo número da issue (menor primeiro)
3. Issues sem fase — ordenar por label (bug primeiro, enhancement depois)
4. PRs abertos — listar separadamente

---

## Etapa 6 — Aguardar instrução do usuário

Após apresentar a lista, **não iniciar nenhuma implementação automaticamente**.

Perguntar ao usuário: "Por onde quer começar?"

Só então seguir o workflow de desenvolvimento normal (criar branch, briefing, aprovação, etc.).

---

## Observações importantes

- Nunca assumir que uma issue está implementada apenas pelo título — verificar o código
- Se houver PRs abertos, mencionar que há trabalho em andamento antes de propor novo trabalho
- Issues de bug têm prioridade sobre features da mesma fase
- Issues do Dependabot (atualizações de dependências) não precisam de análise de código — apenas listar se houver muitas acumuladas
