# Skills — agentfiles

Skills são blocos de conhecimento especializado ativados por contexto ou comando explícito.

## Skills disponíveis

| Skill | Ativar com | Quando usar |
|-------|-----------|-------------|
| `code-review.md` | `/review` ou mencionando "revisar código" | Code review estruturado com checklist |
| `spec-driven-development.md` | `/spec` ou "criar spec" | Desenvolvimento guiado por especificação |
| `systematic-debugging.md` | `/debug` ou "investigar bug" | Diagnóstico sistemático de problemas |
| `architecture-design.md` | `/arch` ou "decisão de arquitetura" | Design e documentação arquitetural |

## Como usar

```
# Ativar uma skill específica
/review [arquivo ou trecho de código]
/spec [nome da feature]
/debug [descrição do problema]
/arch [decisão a tomar]

# Ou descrever naturalmente
"Revise este código seguindo as boas práticas do projeto"
"Preciso criar uma spec para o módulo de pagamentos"
"Estou com um bug de concorrência que não consigo reproduzir"
```
