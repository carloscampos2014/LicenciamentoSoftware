# Arquitetura

O LicenseManager é construído sobre uma arquitetura em camadas com isolamento por tenant.

## Visão geral

```
┌─────────────────────────────────────────────────────────┐
│                    Interfaces de Acesso                  │
│  Blazor WASM (Web)  │  MAUI Windows  │  MAUI Android    │
└──────────────────────────────┬──────────────────────────┘
                               │ JWT + HMAC
┌──────────────────────────────▼──────────────────────────┐
│                    API REST (ASP.NET Core)               │
│  Controllers  │  Middleware HMAC  │  Anti-Replay         │
└──────────────────────────────┬──────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────┐
│                    Application Layer                     │
│  Handlers  │  Commands  │  Validators                   │
└──────────────────────────────┬──────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────┐
│                    Domain Layer                          │
│  Entities  │  Value Objects  │  Domain Events            │
└──────────────────────────────┬──────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────┐
│                 Infrastructure Layer                     │
│  Dapper  │  EF Core  │  PostgreSQL  │  Repositories      │
└─────────────────────────────────────────────────────────┘
```

## Projetos da solução

| Projeto | Responsabilidade |
|---|---|
| `Domain` | Entidades, value objects, invariantes de negócio |
| `Application` | Handlers, commands, validators (sem dependência de infra) |
| `Infrastructure` | Repositórios Dapper/EF Core, PostgreSQL |
| `Api` | Controllers REST, middleware, configuração de DI |
| `Client` | HttpClient compartilhado entre Web e MAUI |
| `Web` | Blazor WASM + BFF (ASP.NET Core) |
| `Maui` | App desktop/mobile multiplataforma |
| `Admin` | Painel de monitoramento interno (Basic Auth, SSH tunnel) |

## Segurança

- **Autenticação:** JWT com claims de tenant e papel
- **2FA:** TOTP (Google Authenticator / Authy)
- **Validação:** HMAC-SHA256 com timestamp e nonce (proteção anti-replay ±5 min)
- **Isolamento:** `IdCliente` sempre do JWT, nunca do body
- **Auditoria:** log transacional de todas as operações de escrita

## Banco de dados

PostgreSQL hospedado na mesma Oracle Cloud VM da API.
Migrations via EF Core, acesso de leitura via Dapper.

## Deploy

| Componente | Infraestrutura |
|---|---|
| API REST | Oracle Cloud VM (Ubuntu), systemd, Nginx reverse proxy |
| Blazor Web | Mesmo servidor, build WASM estático servido pelo Nginx |
| Banco | PostgreSQL local na VM |
| CDN/SSL | Cloudflare (proxy + certificado TLS) |
