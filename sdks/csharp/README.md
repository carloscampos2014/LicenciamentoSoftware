# LicenseManagerSdk — C#/.NET

SDK cliente para a API de validação do LicenseManager.

## Instalação

```bash
dotnet add package LicenseManagerSdk
```

## Uso

```csharp
using LicenseManagerSdk;

var client = new LicenseManagerClient(
    baseUrl:   "https://licensemanager-api.enzojb.com.br",
    token:     "seu-token-de-licenca",
    licenseId: "guid-da-licenca"
);

// Validar login de usuário
var login = await client.LoginAsync("usuario@empresa.com");
if (login.Authorized)
{
    var sessionId = login.SessionId!;

    // Heartbeat periódico (a cada N minutos, conforme TempoLimiteSessaoHoras da licença)
    await client.HeartbeatAsync(sessionId);

    // Logout ao fechar o app
    await client.LogoutAsync(sessionId);
}

// Validar instalação na máquina
var installation = await client.ValidateInstallationAsync(
    machineId: Environment.MachineName
);
if (installation.Authorized)
{
    Console.WriteLine($"Instalação registrada: {installation.InstallationId}");
}
```

## Targets suportados

- .NET 6.0
- .NET 8.0
- .NET 10.0

## Testes

```bash
dotnet test sdks/csharp/LicenseManagerSdk.Tests
```
