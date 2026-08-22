---
description: "Result Pattern obrigatório — nunca retornar null, lançar exceção para fluxo previsível ou engolir erros silenciosamente."
---

# Result Pattern

Todo método que pode falhar de forma previsível deve retornar um Result, nunca lançar exceção nem retornar null.

## Implementação C#

```csharp
// Result genérico
public class Result<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public IReadOnlyList<string> Errors { get; private set; } = [];
    public IReadOnlyList<string> Warnings { get; private set; } = [];
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    public static Result<T> Ok(T data) =>
        new() { Success = true, Data = data };

    public static Result<T> Fail(string error) =>
        new() { Success = false, Errors = [error] };

    public static Result<T> Fail(IEnumerable<string> errors) =>
        new() { Success = false, Errors = errors.ToList() };

    public static Result<T> WithWarning(T data, string warning) =>
        new() { Success = true, Data = data, Warnings = [warning] };
}

// Result sem dado
public class Result : Result<object?>
{
    public static Result Ok() => new() { Success = true };
    public static new Result Fail(string error) => new() { Success = false, Errors = [error] };
}
```

## Implementação TypeScript

```typescript
interface Result<T> {
  success: boolean;
  data?: T;
  errors: string[];
  warnings: string[];
  timestamp: string;
}

function ok<T>(data: T): Result<T> {
  return { success: true, data, errors: [], warnings: [], timestamp: new Date().toISOString() };
}

function fail<T>(errors: string | string[]): Result<T> {
  const list = Array.isArray(errors) ? errors : [errors];
  return { success: false, errors: list, warnings: [], timestamp: new Date().toISOString() };
}
```

## NotificationContext — acumular todos os erros antes de retornar

Nunca retornar na primeira falha de validação. Coletar todos os erros:

```csharp
public class NotificationContext
{
    private readonly List<string> _errors = [];
    private readonly List<string> _warnings = [];

    public bool HasErrors => _errors.Count > 0;
    public IReadOnlyList<string> Errors => _errors;

    public void AddError(string message) => _errors.Add(message);
    public void AddWarning(string message) => _warnings.Add(message);

    public Result<T> ToResult<T>(T? data = default) =>
        HasErrors
            ? Result<T>.Fail(_errors)
            : Result<T>.Ok(data!);
}

// Uso no handler/service
public async Task<Result<PedidoDto>> CriarPedidoAsync(CriarPedidoCommand cmd)
{
    var ctx = new NotificationContext();

    if (string.IsNullOrWhiteSpace(cmd.ClienteId))
        ctx.AddError("ClienteId é obrigatório");

    if (cmd.Itens.Count == 0)
        ctx.AddError("Pedido deve ter ao menos um item");

    if (ctx.HasErrors)
        return ctx.ToResult<PedidoDto>();

    var pedido = new Pedido(cmd.ClienteId, cmd.Itens);
    await _repository.AddAsync(pedido);

    return Result<PedidoDto>.Ok(pedido.ToDto());
}
```

## Regras

```
✅ Retornar Result<T> de todo método que pode falhar previsivelmente
✅ Coletar TODOS os erros antes de retornar (NotificationContext)
✅ Controller mapeia Result → HTTP status (200/400/404/409/500)
✅ Usar Result.Fail() para erros de negócio
✅ Reservar exceptions para erros inesperados de infraestrutura

❌ throw new Exception() para validação de entrada
❌ Retornar null para "não encontrado" — usar Result.Fail("não encontrado")
❌ catch (Exception e) { } — engolir silenciosamente
❌ Retornar false sem mensagem de erro
❌ Múltiplos return na primeira falha de validação
```

## Controller mapeando Result → HTTP

```csharp
[HttpPost]
public async Task<IActionResult> Criar([FromBody] CriarPedidoCommand cmd)
{
    var result = await _handler.CriarPedidoAsync(cmd);

    return result.Success
        ? Ok(result.Data)
        : BadRequest(new { errors = result.Errors });
}
```
