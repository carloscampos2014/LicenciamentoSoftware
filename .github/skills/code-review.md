# Skill: Code Review

Revisão estruturada de código cobrindo qualidade, segurança, arquitetura e testes.

## Como ativar

`/review` ou descrever "revisar", "analisar código", "code review"

## Processo de revisão

### Etapa 1 — Ler antes de julgar

Antes de apontar qualquer problema:
1. Entender o que o código tenta fazer
2. Ler o contexto ao redor (arquivos relacionados)
3. Verificar se existe spec ou requisito documentado
4. Identificar restrições do projeto (libs proibidas, padrões obrigatórios)

### Etapa 2 — Checar por categoria

**Arquitetura e Design**
- [ ] Dependency Rule respeitada (camadas internas não conhecem externas)
- [ ] Responsabilidade única (SRP) — cada classe/método faz uma coisa
- [ ] Interfaces em Application, implementações em Infrastructure
- [ ] Controller thin — sem lógica de negócio
- [ ] Result Pattern aplicado em métodos que podem falhar

**Clean Code**
- [ ] Nomes revelam intenção (sem `data`, `temp`, `manager` vagos)
- [ ] Métodos com menos de 30 linhas
- [ ] Sem magic numbers — constantes nomeadas
- [ ] Nesting máximo de 3 níveis
- [ ] Sem código comentado ou TODOs sem prazo

**Segurança**
- [ ] Sem secrets, tokens ou connection strings inline
- [ ] Queries parametrizadas (sem concatenação com input do usuário)
- [ ] Validação de entrada antes de processar
- [ ] Autorização verificada antes de acessar dados sensíveis
- [ ] Dados de outro tenant nunca acessíveis

**Testes**
- [ ] Novos comportamentos cobertos por testes
- [ ] Testes com nomes `Metodo_Cenario_Resultado`
- [ ] Caminhos de erro testados (não só caminho feliz)
- [ ] Sem mocks que sempre retornam sucesso

**Performance**
- [ ] Sem queries N+1
- [ ] Paginação em listagens
- [ ] Operações pesadas assíncronas
- [ ] Sem bloqueios desnecessários em seções críticas

### Etapa 3 — Classificar severidade

| Severidade | Critério | Ação |
|------------|----------|------|
| 🔴 Crítico | Segurança, dado incorreto, build quebrado | Bloquear — deve corrigir |
| 🟡 Atenção | Violação de padrão, cobertura insuficiente | Deve corrigir antes do merge |
| 🔵 Sugestão | Melhoria desejável, refactoring futuro | Pode corrigir depois |
| ✅ Elogio | Boa decisão, código exemplar | Reconhecer explicitamente |

## Formato do relatório

```
📋 Code Review — [arquivo/PR/feature]

### Resumo
[2-3 frases sobre a qualidade geral]

### Problemas encontrados

🔴 Crítico ([N])
- [arquivo:linha] — [problema] | [como corrigir]

🟡 Atenção ([N])
- [arquivo:linha] — [problema] | [sugestão]

🔵 Sugestão ([N])
- [arquivo:linha] — [melhoria possível]

✅ Pontos positivos
- [o que foi bem feito]

### Veredicto
[✅ Aprovado / 🟡 Aprovado com ressalvas / 🔴 Requer correções]

### Próximas ações obrigatórias
- [ ] [ação específica]
- [ ] [ação específica]
```

## Exemplos de problemas comuns

```csharp
// 🔴 SQL Injection — crítico
var sql = $"SELECT * FROM users WHERE email = '{email}'"; // NUNCA

// ✅ Correto
var sql = "SELECT * FROM users WHERE email = @Email";
await conn.QueryAsync(sql, new { Email = email });

// 🟡 N+1 — atenção
var pedidos = await repo.GetAllAsync();
foreach (var p in pedidos)
    p.Cliente = await clienteRepo.GetByIdAsync(p.ClienteId); // N queries!

// 🔴 Exception para fluxo de negócio
throw new Exception("Usuário não encontrado"); // usar Result.Fail()

// 🟡 Magic number
if (desconto > 0.5m) // o que é 0.5? usar constante MAX_DESCONTO_PERMITIDO
```
