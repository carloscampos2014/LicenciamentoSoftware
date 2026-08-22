---
description: "Boas práticas de banco de dados — índices, queries seguras, paginação, soft delete, auditoria e migrations."
---

# Boas Práticas de Banco de Dados

## Regras Fundamentais

```
✅ Parameterized queries — sempre, sem exceção
✅ Soft delete com coluna deleted_at ou ativo
✅ Timestamps created_at / updated_at em toda tabela
✅ Paginação em toda query que pode retornar múltiplos registros
✅ Transactions para operações multi-tabela
✅ Migrations versionadas e reversíveis
✅ Índices em foreign keys, colunas de busca frequente e unique constraints

❌ SQL concatenado com strings do usuário (SQL Injection)
❌ SELECT * em produção
❌ DELETE físico de registros de negócio
❌ Queries N+1 (buscar entidade + loop buscando relacionamentos)
❌ Migrations irreversíveis sem backup prévio
❌ Connection strings no código ou repositório
```

## Estrutura de tabela padrão

```sql
CREATE TABLE pedidos (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    cliente_id  UUID        NOT NULL REFERENCES clientes(id),
    status      VARCHAR(20) NOT NULL DEFAULT 'pendente',
    total       NUMERIC(12,2) NOT NULL,
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    deleted_at  TIMESTAMPTZ,           -- soft delete
    created_by  UUID        REFERENCES usuarios(id)
);

-- Índices obrigatórios
CREATE INDEX idx_pedidos_cliente_id ON pedidos(cliente_id);
CREATE INDEX idx_pedidos_status     ON pedidos(status) WHERE deleted_at IS NULL;
CREATE INDEX idx_pedidos_created_at ON pedidos(created_at DESC);
```

## Auditoria com trigger PostgreSQL

```sql
CREATE TABLE audit_log (
    id          BIGSERIAL   PRIMARY KEY,
    tabela      VARCHAR(100) NOT NULL,
    operacao    VARCHAR(10)  NOT NULL, -- INSERT / UPDATE / DELETE
    registro_id UUID,
    dados_antes JSONB,
    dados_depois JSONB,
    usuario_id  UUID,
    ip_address  INET,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE OR REPLACE FUNCTION audit_trigger_fn() RETURNS trigger AS $$
BEGIN
    INSERT INTO audit_log(tabela, operacao, registro_id, dados_antes, dados_depois)
    VALUES (
        TG_TABLE_NAME,
        TG_OP,
        COALESCE(NEW.id, OLD.id),
        CASE WHEN TG_OP = 'INSERT' THEN NULL ELSE row_to_json(OLD) END,
        CASE WHEN TG_OP = 'DELETE' THEN NULL ELSE row_to_json(NEW) END
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Aplicar na tabela
CREATE TRIGGER audit_pedidos
AFTER INSERT OR UPDATE OR DELETE ON pedidos
FOR EACH ROW EXECUTE FUNCTION audit_trigger_fn();
```

## Paginação obrigatória

```csharp
// Query paginada com Dapper
public async Task<PagedResult<PedidoDto>> ListarAsync(int page, int pageSize)
{
    const string sql = """
        SELECT id, cliente_id, status, total, created_at
        FROM pedidos
        WHERE deleted_at IS NULL
        ORDER BY created_at DESC
        LIMIT @PageSize OFFSET @Offset;

        SELECT COUNT(*) FROM pedidos WHERE deleted_at IS NULL;
        """;

    using var multi = await _connection.QueryMultipleAsync(sql, new {
        PageSize = pageSize,
        Offset = (page - 1) * pageSize
    });

    var items = (await multi.ReadAsync<PedidoDto>()).ToList();
    var total = await multi.ReadFirstAsync<int>();

    return new PagedResult<PedidoDto>(items, total, page, pageSize);
}
```

## Prevenção de N+1

```csharp
// ❌ N+1 — 1 query para pedidos + N queries para clientes
var pedidos = await _repo.GetAllAsync();
foreach (var p in pedidos)
    p.Cliente = await _clienteRepo.GetByIdAsync(p.ClienteId); // N queries!

// ✅ JOIN em uma só query
const string sql = """
    SELECT p.*, c.nome as cliente_nome
    FROM pedidos p
    JOIN clientes c ON c.id = p.cliente_id
    WHERE p.deleted_at IS NULL
    """;
```

## Migrations (DbUp ou EF Core)

```
migrations/
  V001__create_usuarios.sql
  V002__create_pedidos.sql
  V003__add_index_pedidos_status.sql
  V004__add_audit_log.sql
```

Regras de migration:
- Sempre reversível (ter script de rollback documentado)
- Nunca dropar coluna com dados sem migrar antes
- Adicionar colunas nullable ou com DEFAULT para não bloquear tabela
- Testar migration em base de dev antes de produção

## Connection String — via variável de ambiente

```csharp
// appsettings.json — sem valor real
"ConnectionStrings": { "Default": "" }

// Program.cs
builder.Services.AddNpgsqlDataSource(
    builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string não configurada"));
```

```
# .env (nunca no repositório)
ConnectionStrings__Default=Host=localhost;Database=meuapp_dev;Username=postgres;Password=...
```
