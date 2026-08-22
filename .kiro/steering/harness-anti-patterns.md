---
inclusion: always
description: "Comportamentos proibidos do agente — anti-patterns que causam retrabalho e frustração"
---

# Anti-Patterns do Agente — Comportamentos Proibidos

## AP-01: Pular Pesquisa Prévia

```
❌ "É simples, vou direto para as perguntas"
✅ Sempre ler o código e docs relevantes antes de perguntar
```
Perguntas genéricas que o codebase já responde desperdiçam o tempo do usuário.

## AP-02: Gerar Spec Sem Interação

```
❌ [Gera requirements.md completo sem conversar]
✅ Coletar requisitos pergunta a pergunta, validar com usuário
```
Spec baseado em suposições garante retrabalho.

## AP-03: Implementar Feature Complexa Sem Spec

```
❌ "Vou implementar direto, é mais rápido"
✅ "Feature complexa detectada. Criar spec primeiro?"
```
Sem spec = sem critérios de aceite = sem como validar.

## AP-04: Workaround em Vez de Fix

```
❌ try { } catch { }  // engolir exceção
❌ if (valor == null) return []  // esconder o problema
✅ Diagnosticar causa raiz → corrigir → verificar
```

## AP-05: Clamar "Concluído" Sem Evidência

```
❌ "Task concluída, tudo funcionando"
✅ Mostrar output real de build + testes
```

## AP-06: Responder a Própria Pergunta

```
❌ "Qual módulo? Provavelmente é o X. Vou assumir X."
✅ "Qual módulo?" → PARAR e aguardar resposta do usuário
```

## AP-07: Re-Analisar Código Já Analisado

```
❌ [Lê 2000 linhas que já tem docs em .kiro/knowledge/]
✅ Verificar cache primeiro: .kiro/knowledge/patterns/
```

## AP-08: Commit de Código que Não Compila

```
❌ git commit sem rodar build
✅ build → testes → só então commit
```

## AP-09: Ignorar Hooks que Falharam

```
❌ Hook: ❌ REPROVADO → [prossegue mesmo assim]
✅ Hook: ❌ REPROVADO → corrigir antes de prosseguir
```

## AP-10: Scope Creep Silencioso

```
❌ [Refatora, renomeia e "melhora" coisas fora do escopo]
✅ Implementar exatamente o que foi pedido; mencionar melhorias possíveis separadamente
```

## AP-11: Verificação Teatral

```
❌ "Deve funcionar agora" sem executar
✅ Mostrar output real observado, não inferido
```

## AP-12: Assumir Sem Verificar

```
❌ "O endpoint provavelmente retorna X"
✅ Abrir o arquivo e confirmar antes de afirmar
```
