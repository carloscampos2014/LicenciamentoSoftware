---
inclusion: always
description: "Protocolo de uma pergunta por vez — como coletar requisitos sem bombardear o usuário"
---

# Protocolo: Uma Pergunta por Vez

## Regra Principal

Nunca fazer mais de uma pergunta ao usuário por mensagem.

Se precisar de múltiplas informações, priorize pela mais bloqueante e pergunte apenas ela.
Quando a resposta chegar, faça a próxima se ainda necessário.

---

## Formato de Pergunta com Opções

Sempre que possível, oferecer opções numeradas em vez de campo aberto:

```
[Contexto de 1-2 frases explicando por que precisa saber]

[Pergunta direta]

1. [Opção A] — [descrição breve]
2. [Opção B] — [descrição breve]
3. [Opção C] — [descrição breve]
4. Outro: [campo livre]

Recomendação: opção [N] porque [razão de 1 frase].
```

---

## Quando Perguntar vs. Agir

| Situação | Ação |
|----------|------|
| Informação está no código/docs | Buscar, não perguntar |
| 2+ interpretações plausíveis e irreversíveis | Perguntar |
| Ambiguidade resolve-se lendo o contexto | Ler o contexto |
| Decisão de design com tradeoffs reais | Apresentar opções com recomendação |
| Pedido claro e reversível | Agir diretamente |

---

## Anti-pattern a Evitar

```
❌ "Qual é o nome do projeto? Qual stack você usa? Tem testes? 
    Prefere Clean Architecture ou CQRS? Vai usar Docker?"

✅ "Qual é o nome do projeto?"
   [aguarda resposta]
   "Stack principal?"
   [aguarda resposta]
```
