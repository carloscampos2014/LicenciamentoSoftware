---
description: "Requisitos de testes — cobertura mínima, nomenclatura, estrutura AAA, frameworks por stack e testes de integração."
---

# Requisitos de Testes

## Cobertura Mínima

| Camada | Cobertura mínima |
|--------|-----------------|
| Domain (regras de negócio) | 95%+ |
| Application (use cases, services) | 80%+ |
| Infrastructure | 60%+ (integração) |
| Presentation (controllers, endpoints) | 70%+ |

## Nomenclatura

Formato obrigatório: `Metodo_Cenario_ResultadoEsperado`

```
✅ CriarPedido_QuantidadeZero_RetornaErro
✅ Login_CredenciaisValidas_RetornaToken
✅ CalcularDesconto_ClienteVip_AplicaDesconto20Porcento
✅ BuscarUsuario_IdInexistente_RetornaNotFound

❌ TesteCriarPedido
❌ TesteLogin1
❌ Deve_Funcionar
```

## Estrutura AAA (Arrange / Act / Assert)

```csharp
[Fact]
public async Task CriarPedido_ItensVazios_RetornaErroDeValidacao()
{
    // Arrange
    var repositoryMock = Substitute.For<IPedidoRepository>();
    var handler = new CriarPedidoHandler(repositoryMock);
    var command = new CriarPedidoCommand(ClienteId: "cliente-1", Itens: []);

    // Act
    var result = await handler.HandleAsync(command);

    // Assert
    result.Success.Should().BeFalse();
    result.Errors.Should().Contain("Pedido deve ter ao menos um item");
    await repositoryMock.DidNotReceive().AddAsync(Arg.Any<Pedido>());
}
```

## Stack .NET — xUnit + FluentAssertions + NSubstitute

```xml
<!-- .csproj -->
<PackageReference Include="xunit" Version="2.9.*" />
<PackageReference Include="FluentAssertions" Version="6.*" />
<PackageReference Include="NSubstitute" Version="5.*" />
<PackageReference Include="Testcontainers.PostgreSql" Version="3.*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.*" />
```

```csharp
// Teste de integração com Testcontainers
public class PedidoRepositoryTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder().Build();

    public async Task InitializeAsync() => await _postgres.StartAsync();
    public async Task DisposeAsync() => await _postgres.DisposeAsync();

    [Fact]
    public async Task SalvarPedido_DadosValidos_PersisteMesmoAposRestart()
    {
        // Arrange
        await using var conn = new NpgsqlConnection(_postgres.GetConnectionString());
        var repo = new PedidoRepository(conn);
        var pedido = new Pedido("cliente-1", [new Item("prod-1", 2)]);

        // Act
        await repo.AddAsync(pedido);
        var encontrado = await repo.GetByIdAsync(pedido.Id);

        // Assert
        encontrado.Should().NotBeNull();
        encontrado!.ClienteId.Should().Be("cliente-1");
    }
}
```

## Stack TypeScript — Jest + Testing Library

```typescript
// jest.config.ts
export default {
  preset: 'ts-jest',
  testEnvironment: 'node',
  coverageThreshold: {
    global: { branches: 80, functions: 80, lines: 80, statements: 80 }
  }
};

// Exemplo de teste de serviço
describe('PedidoService', () => {
  let service: PedidoService;
  let repoMock: jest.Mocked<IPedidoRepository>;

  beforeEach(() => {
    repoMock = { save: jest.fn(), findById: jest.fn() } as any;
    service = new PedidoService(repoMock);
  });

  it('criarPedido_itensVazios_retornaErro', async () => {
    const result = await service.criarPedido({ clienteId: 'c1', itens: [] });
    expect(result.success).toBe(false);
    expect(result.errors).toContain('Pedido deve ter ao menos um item');
    expect(repoMock.save).not.toHaveBeenCalled();
  });
});
```

## Proibições

```
❌ Testes que apenas verificam que "não lança exceção"
❌ Mocks que retornam sempre sucesso sem testar o caminho de falha
❌ Testes dependentes de ordem de execução
❌ Testes com dados reais (usar Bogus/faker para gerar dados)
❌ Assert de apenas uma coisa quando o teste tem múltiplos comportamentos
❌ Thread.Sleep / setTimeout em testes — usar async/await ou fake timers
❌ Desabilitar testes com skip sem comentário explicando por quê e quando resolver
```
