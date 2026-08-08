# SDK C# / .NET

[![NuGet](https://img.shields.io/nuget/v/LicenseManagerSdk)](https://www.nuget.org/packages/LicenseManagerSdk/)

## Instalação

```bash
dotnet add package LicenseManagerSdk
```

## Targets suportados

- .NET 6.0
- .NET 8.0
- .NET 10.0

## Uso básico

```csharp
using LicenseManagerSdk;

var client = new LicenseManagerClient(
    baseUrl:   "https://licensemanager-api.enzojb.com.br",
    token:     "seu-token",
    licenseId: "guid-da-licenca"
);

// Login
var login = await client.LoginAsync("usuario@empresa.com");
if (login.Authorized)
{
    // Heartbeat periódico
    await client.HeartbeatAsync(login.SessionId!);

    // Logout ao fechar
    await client.LogoutAsync(login.SessionId!);
}

// Validar instalação
var inst = await client.ValidateInstallationAsync(Environment.MachineName);
if (inst.Authorized)
    Console.WriteLine($"Instalação: {inst.InstallationId}");
```

## Injeção de dependência (ASP.NET Core)

```csharp
builder.Services.AddSingleton(new LicenseManagerClient(
    baseUrl:   builder.Configuration["LicenseManager:Url"]!,
    token:     builder.Configuration["LicenseManager:Token"]!,
    licenseId: builder.Configuration["LicenseManager:LicenseId"]!
));
```
