---
name: code-review
description: Revisão estruturada de código cobrindo qualidade, segurança, arquitetura e testes. Ativar com /code-review ou mencionando "revisar código".
---

# Skill: Code Review

## Processo

### Etapa 1 — Ler antes de julgar
1. Entender o que o código tenta fazer
2. Ler contexto ao redor (arquivos relacionados)
3. Verificar se há spec ou requisito documentado

### Etapa 2 — Checar por categoria

**Arquitetura**
- [ ] Dependency Rule respeitada (camadas internas não conhecem externas)
- [ ] SRP — cada classe/método tem uma responsabilidade
- [ ] Interfaces em Application, implementações em Infrastructure
- [ ] Controller thin — sem lógica de negócio
- [ ] Result Pattern em métodos que podem falhar

**Clean Code**
- [ ] Nomes revelam intenção (sem `data`, `temp`, `manager` vagos)
- [ ] Métodos < 30 linhas
- [ ] Sem magic numbers — constantes nomeadas
- [ ] Nesting máximo 3 níveis
- [ ] Sem código comentado ou TODOs sem prazo

**Segurança**
- [ ] Sem secrets ou connection strings inline
- [ ] Queries parametrizadas (sem concatenação com input do usuário)
- [ ] Validação de entrada antes de processar
- [ ] Isolamento de tenant — dados de outro usuário nunca acessíveis

**Testes**
- [ ] Novos comportamentos cobertos
- [ ] Nomes `Metodo_Cenario_Resultado`
- [ ] Caminhos de erro testados
- [ ] Sem mocks que sempre retornam sucesso

**Performance**
- [ ] Sem queries N+1
- [ ] Paginação em listagens
- [ ] Operações pesadas assíncronas

### Etapa 3 — Classificar severidade

| Severidade | Critério | Ação |
|------------|----------|------|
| 🔴 Crítico | Segurança, dado incorreto, build quebrado | Bloquear |
| 🟡 Atenção | Violação de padrão, cobertura insuficiente | Corrigir antes do merge |
| 🔵 Sugestão | Melhoria desejável | Pode corrigir depois |
| ✅ Elogio | Boa decisão | Reconhecer |

## Formato do relatório

```
📋 Code Review — [arquivo/PR]

### Resumo
[2-3 frases sobre qualidade geral]

🔴 Crítico ([N])
- [arquivo:linha] — [problema] | [como corrigir]

🟡 Atenção ([N])
- [arquivo:linha] — [problema] | [sugestão]

🔵 Sugestão ([N])
- [melhoria possível]

✅ Pontos positivos
- [o que foi bem feito]

### Veredicto: ✅ Aprovado / 🟡 Com ressalvas / 🔴 Requer correções
```
