# Skill: Systematic Debugging

Diagnóstico estruturado de bugs — da hipótese à correção verificada.

## Como ativar

`/debug` ou mencionar "investigar bug", "não consigo reproduzir", "erro estranho"

## Processo em 6 etapas

### Etapa 1 — Definir o problema com precisão

Antes de qualquer diagnóstico, responder:
- **O que acontece?** (comportamento observado)
- **O que deveria acontecer?** (comportamento esperado)
- **Quando acontece?** (condições, frequência, ambiente)
- **Quando NÃO acontece?** (o que diferencia os casos que falham dos que não falham)
- **Desde quando?** (última versão que funcionava)

```
❌ "O sistema está lento"
✅ "A listagem de pedidos demora >5s em produção quando há >1000 registros,
    mas é rápida em dev com <100 registros. Começou após deploy da v2.3."
```

### Etapa 2 — Reproduzir de forma isolada

Um bug não reproduzível não pode ser corrigido de forma confiável.

```
✅ Criar teste que reproduz o bug
✅ Isolar variáveis (remover dependências externas)
✅ Verificar em ambiente idêntico ao de produção
✅ Coletar logs no momento exato da falha

❌ "Corrigir" sem conseguir reproduzir
❌ Assumir que foi corrigido sem teste que prove
```

### Etapa 3 — Formular hipóteses (do mais para o menos provável)

```
Hipótese 1: [causa mais provável] — evidência: [dado que suporta]
Hipótese 2: [causa alternativa]   — evidência: [dado que suporta]
Hipótese 3: [causa edge case]     — evidência: [dado que suporta]
```

Critérios para priorizar hipóteses:
- Mudanças recentes no código (commits últimos 7 dias)
- Padrão do erro (sempre / às vezes / sob carga / em horários específicos)
- Stack trace / logs disponíveis
- Semelhança com bugs anteriores

### Etapa 4 — Testar hipóteses

Testar uma hipótese por vez. Nunca múltiplas correções simultaneamente.

```
Hipótese 1 testada:
  Método: [como foi testada]
  Resultado: [confirmou / refutou]
  Evidência: [log, output, teste]

Se confirmou → ir para etapa 5
Se refutou   → testar próxima hipótese
```

Hard bound: após 3 hipóteses refutadas sem progresso, parar e reportar ao usuário.

### Etapa 5 — Aplicar a correção mínima

```
✅ Menor mudança que corrige a causa raiz
✅ Não aproveitar para refactoring paralelo
✅ Manter o teste que reproduzia o bug (agora deve passar)

❌ Workaround que esconde o sintoma sem corrigir a causa
❌ try { } catch { } para suprimir o erro
❌ if (valor == null) return; sem entender por que é null
```

### Etapa 6 — Twin Check + verificação

```bash
# Buscar o mesmo padrão em todo o projeto
grep -r "padrão-do-bug" src/

# Resultado obrigatório no report:
TWINS: busquei <padrão> — encontrei <N> outros locais: <arquivos ou "nenhum">
```

Verificar que:
- O teste que reproduzia o bug agora passa
- Testes existentes continuam passando
- Build limpo

## Debugging por tipo de problema

### Bug de lógica (resultado errado)
```
1. Identificar o método que produz o resultado errado
2. Adicionar assertions/logs nos inputs e outputs
3. Rastrear de fora para dentro até encontrar onde o dado diverge
4. Verificar se regra de negócio está implementada corretamente
```

### Bug de concorrência (race condition)
```
1. Identificar recursos compartilhados (estado, banco, cache)
2. Verificar se há transação em operações multi-step
3. Verificar se async/await está correto (sem .Result ou .Wait())
4. Buscar por lock/semáforo ausente em seções críticas
```

### Bug de performance (lento)
```
1. Medir tempo por seção (não adivinhar onde está)
2. Verificar queries N+1 (checar logs do banco)
3. Verificar se há await dentro de loop
4. Verificar índices ausentes nas queries mais lentas
5. Verificar se há cache sendo ignorado/invalidado incorretamente
```

### Bug intermitente (às vezes falha)
```
1. Aumentar logging temporariamente
2. Verificar se depende de hora/data/timezone
3. Verificar se depende de dados específicos (buscar o que é diferente)
4. Verificar race conditions e timeouts
5. Verificar memory leaks em sessões longas
```

## Formato do relatório

```
🔍 Diagnóstico de Bug — [título]

**Problema:** [descrição precisa]
**Reproduzível:** ✅ Sim / ❌ Não

**Hipóteses testadas:**
1. [hipótese] → [confirmou/refutou] — [evidência]
2. [hipótese] → [confirmou/refutou] — [evidência]

**Causa raiz identificada:**
[descrição da causa real]

**Correção aplicada:**
- [arquivo:linha] — [o que mudou e por que]

**Teste adicionado:**
- [NomeDoTeste] — reproduz o cenário do bug

**Verificação:**
- Build: ✅ OK
- Testes: ✅ [N] passando
- TWINS: busquei [padrão] — [N outros locais ou "nenhum"]
```
