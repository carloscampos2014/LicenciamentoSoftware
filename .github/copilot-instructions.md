# LicenciamentoSoftware — Instruções para GitHub Copilot

Projeto: Sistema de licenciamento de software com .NET 10, Clean Architecture, PostgreSQL, Dapper e DbUp.
Stack: .NET 10, C#, ASP.NET Core, Blazor WASM, .NET MAUI, PostgreSQL, Dapper, DbUp, xUnit

---

## 🎯 Regras de Ouro (OBRIGATÓRIAS)

1. **Ler antes de escrever** — inspecionar o código existente antes de sugerir qualquer coisa
2. **SOLID obrigatório** — SRP, OCP, LSP, ISP, DIP em toda sugestão
3. **Nomes revelam intenção** — nunca sugerir `data`, `temp`, `manager`, `helper` sem qualificador
4. **Sem números mágicos** — sempre usar constantes nomeadas
5. **Result Pattern** — erros de negócio retornam `Result<T>`, nunca lançam exceção
6. **Tratamento explícito** — nunca `catch {}` vazio; sempre tratar ou relançar com contexto
7. **Sem secrets no código** — nunca connection strings, tokens ou senhas inline
8. **Métodos curtos e focados** — extrair quando ultrapassar ~30 linhas de lógica
9. **Testes para nova lógica** — sugerir teste junto com implementação
10. **Build sem warnings** — corrigir todos os warnings, não suprimi-los com `#pragma`

---

## 🚨 Anti-Patterns (O que NUNCA sugerir)

**Design:**
- God classes com múltiplas responsabilidades
- Lógica de negócio em controllers, pages ou ViewModels
- Dependência direta de implementações concretas (usar interfaces)
- Serviços estáticos onde injeção de dependência é possível

**Código:**
- `catch (Exception e) { }` — sempre tratar ou relançar com contexto
- Nesting maior que 3 níveis — extrair para métodos com early returns
- Método com mais de 3 parâmetros sem objeto de valor/DTO
- Herança onde composição resolve melhor

**Segurança:**
- SQL concatenado com strings de usuário — sempre parameterized queries
- Senhas ou tokens em variáveis sem hashing/env
- `SELECT *` em produção

---

## 🏗️ Padrões de Código

### Nomenclatura

| Elemento | C# | TypeScript/JS | Python |
|----------|----|---------------|--------|
| Classes | PascalCase | PascalCase | PascalCase |
| Métodos/Funções | PascalCase | camelCase | snake_case |
| Variáveis | camelCase | camelCase | snake_case |
| Constantes | UPPER_SNAKE / readonly | UPPER_SNAKE | UPPER_SNAKE |
| Interfaces C# | IPrefixo | — | ABC |
| Arquivos | PascalCase.cs | kebab-case.ts | snake_case.py |

### Result Pattern

```csharp
// ✅ Correto
public async Task<Result<PedidoDto>> CriarAsync(CriarPedidoCommand cmd)
{
    if (string.IsNullOrWhiteSpace(cmd.ClienteId))
        return Result<PedidoDto>.Fail("ClienteId é obrigatório");

    var pedido = new Pedido(cmd.ClienteId, cmd.Itens);
    await _repository.AddAsync(pedido);
    return Result<PedidoDto>.Ok(pedido.ToDto());
}

// ❌ Errado
throw new InvalidOperationException("ClienteId é obrigatório");
```

### Tratamento de erros

```typescript
// ✅ Correto
try {
  const result = await service.criar(dto);
  if (!result.success) return res.status(400).json({ errors: result.errors });
  return res.status(201).json(result.data);
} catch (err) {
  logger.error('Erro inesperado ao criar pedido', err);
  return res.status(500).json({ errors: ['Erro interno'] });
}

// ❌ Errado
try { ... } catch (e) { }  // engolir silenciosamente
```

### Estrutura de método

```csharp
// ✅ Pequeno, focado, nome claro
private bool IsEmailValid(string email)
    => !string.IsNullOrEmpty(email) && email.Contains('@');

// ❌ Faz muitas coisas, nome vago
private void Process(object data) { /* 80 linhas... */ }
```

---

## 🤖 Comandos Disponíveis

| Comando | O que faz |
|---------|-----------|
| `/implement <feature>` | Scaffolding completo com testes |
| `/review` | Code review com SOLID e Clean Code |
| `/fix <bug>` | Diagnóstico + correção + twin check |
| `/refactor` | Refactoring sem mudança de comportamento |
| `/test` | Gerar testes para o código selecionado |
| `/adr <decisao>` | Rascunho de Architecture Decision Record |
| `/spec <feature>` | Criar requirements + design + tasks |
| `/debug <problema>` | Diagnóstico sistemático de bug |
| `/explain` | Explicação do código selecionado |
