---
inclusion: auto
---

# Padrões específicos — LicenciamentoSoftware

## Stack obrigatória

- **Backend:** .NET 10, C#, ASP.NET Core, PostgreSQL, Dapper (sem EF Core), DbUp (migrations)
- **Frontend web:** Blazor WebAssembly + BFF (ASP.NET Core + YARP)
- **Desktop/Mobile:** .NET MAUI (Windows, Android)
- **Testes:** xUnit, FluentAssertions, Testcontainers, NSubstitute, NetArchTest
- **Validação:** FluentValidation
- **Autenticação gestão:** JWT + 2FA TOTP (Google Authenticator / Authy)
- **Autenticação API de validação:** HMAC com timestamp (anti-replay) + token por licença com expiração automática

## Estrutura de projetos

```
src/
  LicenciamentoSoftware.Domain/
  LicenciamentoSoftware.Application/
  LicenciamentoSoftware.Infrastructure/
  LicenciamentoSoftware.Api/
  LicenciamentoSoftware.Client/       ← cliente HTTP compartilhado (Web + MAUI)
  LicenciamentoSoftware.Web/          ← Blazor WASM
  LicenciamentoSoftware.Web.Server/   ← BFF (proxy YARP + cookie HttpOnly)
  LicenciamentoSoftware.Maui/         ← MAUI Desktop (Windows) + Mobile (Android)
tests/
  LicenciamentoSoftware.Domain.Tests/
  LicenciamentoSoftware.Application.Tests/
  LicenciamentoSoftware.IntegrationTests/
  LicenciamentoSoftware.Maui.Tests/
```

## Regras de dependência (verificadas por teste de arquitetura)

- `Application` nunca referencia `Infrastructure` ou `Api`
- `Domain` nunca referencia nenhum outro projeto da solução
- Controllers nunca acessam `DbContext` diretamente

## Isolamento por tenant

- O `IdCliente` (tenant) vem **sempre** da identidade autenticada (`ICurrentUser`), nunca do body da requisição.
- Nenhuma query retorna dados de tenant diferente do usuário autenticado.

## Nomenclatura de casos de uso

Cada agregado tem sua própria pasta em `Application/` com:
- `[Acao][Agregado]Command.cs` ou `[Acao][Agregado]Query.cs`
- `[Acao][Agregado]Validator.cs`
- `[Acao][Agregado]Handler.cs`
- `I[Agregado]Repository.cs`

Exemplo:
```
Application/Clientes/
  CriarClienteCommand.cs
  CriarClienteValidator.cs
  CriarClienteHandler.cs
  IClienteRepository.cs
```

## Regras de negócio obrigatórias

- Exclusão sempre lógica (`Ativo = false`), nunca física.
- Toda operação de escrita gera registro no `LogOperacao`.
- Concorrência em validação de licença deve ser atômica (transação serializável).
- `ClienteFinal` e `Aplicacao` devem pertencer ao mesmo `Cliente` da licença.
- O tipo da aplicação não pode mudar enquanto houver licenças ativas.
- Limites de usuários, sessões e instalações devem ser inteiros positivos.

## Interfaces e distribuição

- **Web:** Blazor WASM + BFF publicado na Oracle Cloud VM (Nginx)
- **Desktop:** MAUI Windows, distribuição via instalador
- **Mobile:** MAUI Android, distribuição via Google Play
- As três interfaces consomem a mesma API REST de gestão
- Lógica de cliente HTTP e modelos compartilhados entre Web e MAUI via `LicenciamentoSoftware.Client`
