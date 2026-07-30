# LicenciamentoSoftware

Sistema de licenciamento de software construído com .NET 10, Clean Architecture, PostgreSQL, Dapper e DbUp.

## Stack

| Camada | Tecnologias |
|---|---|
| Runtime | .NET 10 (LTS) |
| API | ASP.NET Core 10, JWT Bearer, Rate Limiting nativo |
| Banco de dados | PostgreSQL (WSL2 em dev, Supabase/Oracle Cloud em prod) |
| Migrations | DbUp (scripts SQL versionados embarcados no assembly) |
| Queries | Dapper (sem EF Core) |
| Segurança | BCrypt, JWT, TOTP (OTP.NET), HMAC-SHA256 |
| Testes | xUnit, FluentAssertions, NSubstitute, NetArchTest |

## Estrutura de projetos

```
src/
  LicenciamentoSoftware.Domain/           Entidades, value objects, DomainException
  LicenciamentoSoftware.Application/      Handlers, Commands, interfaces de porta
  LicenciamentoSoftware.Infrastructure/   Repositórios Dapper, DbUp, serviços de segurança
  LicenciamentoSoftware.Api/              Controllers, Middleware, configuração DI
  LicenciamentoSoftware.Client/           Cliente HTTP compartilhado (Web + MAUI)
  LicenciamentoSoftware.Web/              Blazor WASM (em desenvolvimento)
  LicenciamentoSoftware.Maui/             App Desktop/Mobile MAUI (em desenvolvimento)

tests/
  LicenciamentoSoftware.Domain.Tests/     Testes unitários de domínio
  LicenciamentoSoftware.Application.Tests/ Testes unitários de handlers e serviços
  LicenciamentoSoftware.IntegrationTests/ Testes de integração (requer PostgreSQL)
```

## Fases concluídas

| Fase | O que foi implementado |
|---|---|
| 1 — Fundação | 7 projetos src + 3 tests, Directory.Build.props, Serilog, ProblemDetails, health check `/health`, docker-compose, testes de arquitetura NetArchTest |
| 2 — Domínio e schema | 11 entidades + 3 value objects (Rich Domain, sem EF Core), DbUp + Dapper, V001_InitialSchema.sql |
| 3 — Identidade e auditoria | JWT + 2FA TOTP, ICurrentUser (tenant do JWT), UnitOfWork Npgsql, AuditLogWriter, endpoints `/auth/*`, V002_UsuarioPapelRefreshToken.sql |
| 4 — Segurança API de validação | Token HMAC-SHA256 por licença, middleware anti-replay (X-Timestamp ±5 min + X-Nonce), rate limiting, `POST /auth/licenca/renovar-token`, V003_LicencaToken.sql |

## Como rodar localmente

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL acessível (WSL2, Docker ou Supabase)

### 1. Configurar a connection string

Copie o arquivo de exemplo e preencha com seus dados:

```powershell
Copy-Item .env.example .env
```

Ou defina diretamente via variável de ambiente:

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=licenciamento;Username=postgres;Password=postgres"
$env:JwtSettings__Secret = "sua-chave-secreta-com-pelo-menos-32-caracteres"
```

### 2. Restaurar dependências e rodar

```powershell
dotnet restore
dotnet run --project src/LicenciamentoSoftware.Api
```

As migrations do DbUp são aplicadas automaticamente na inicialização — não é necessário nenhum comando extra.

### 3. Verificar health check

```
GET http://localhost:5000/health
```

## Docker (PostgreSQL local)

```powershell
# Subir PostgreSQL via docker-compose
docker compose up -d

# Rodar a API (as migrations são aplicadas automaticamente)
dotnet run --project src/LicenciamentoSoftware.Api
```

## PostgreSQL no WSL2

```bash
# Dentro do WSL
sudo service postgresql start
sudo -u postgres psql -c "ALTER USER postgres WITH PASSWORD 'postgres';"
sudo -u postgres createdb licenciamento
```

Connection string para usar do Windows apontando para o WSL2:

```
Host=localhost;Port=5432;Database=licenciamento;Username=postgres;Password=postgres
```

## Rodar os testes

```powershell
# Testes unitários (sem banco)
dotnet test tests/LicenciamentoSoftware.Domain.Tests
dotnet test tests/LicenciamentoSoftware.Application.Tests

# Testes de integração (requer PostgreSQL configurado)
$env:ConnectionStrings__DefaultConnection = "..."
dotnet test tests/LicenciamentoSoftware.IntegrationTests
```

## Endpoints disponíveis

### Autenticação (`/auth`)

| Método | Rota | Descrição |
|---|---|---|
| POST | `/auth/register` | Registra novo usuário |
| POST | `/auth/login` | Login com e-mail e senha |
| POST | `/auth/verify-2fa` | Segunda etapa TOTP |
| POST | `/auth/refresh` | Renova par de tokens JWT |
| POST | `/auth/logout` | Revoga refresh token |
| POST | `/auth/totp/setup` | Configura 2FA TOTP |

### Tokens de licença (`/licencas`)

| Método | Rota | Descrição |
|---|---|---|
| POST | `/licencas/{id}/token` | Emite token HMAC para a licença |
| POST | `/licencas/{id}/token/renovar` | Renova token HMAC (alias: `/auth/licenca/renovar-token`) |

### Monitoramento

| Método | Rota | Descrição |
|---|---|---|
| GET | `/health` | Health check da API |

## Autenticação HMAC para a API de validação

Requisições aos endpoints de validação devem incluir os headers:

```
X-Timestamp: 2026-07-30T12:00:00Z   (ISO-8601 UTC, janela de ±5 min)
X-Nonce: <uuid-ou-string-aleatória>  (máx 128 chars, único por requisição)
```

## Decisões de design

- **Sem EF Core** — DbUp para migrations, Dapper para queries
- **Rich Domain Model** — invariantes no domínio via `DomainException`
- **Sem MediatR** — handlers concretos injetados diretamente nos controllers
- **Tenant isolation** — `IdCliente` sempre do JWT, nunca do body
- **Segredo HMAC** — exibido uma única vez na emissão; apenas o hash BCrypt persiste

## Observações

- Em produção, use variáveis de ambiente ou secrets manager para `JwtSettings:Secret` e a connection string — nunca commite segredos no repositório.
- O `appsettings.json` contém apenas valores padrão vazios para desenvolvimento.
