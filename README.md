# LicenciamentoSoftware

Sistema de licenciamento de software construído com .NET 10, Clean Architecture, PostgreSQL, Dapper e DbUp.

## Stack

| Camada | Tecnologias |
|---|---|
| Runtime | .NET 10 (LTS) |
| API | ASP.NET Core 10, JWT Bearer, Rate Limiting nativo |
| Frontend Web | Blazor WASM + BFF (ASP.NET Core), proxy reverso YARP |
| Banco de dados | PostgreSQL (WSL2 em dev) |
| Migrations | DbUp (scripts SQL versionados embarcados no assembly) |
| Queries | Dapper (sem EF Core) |
| Segurança | BCrypt, JWT, TOTP (OTP.NET), HMAC-SHA256, cookie HttpOnly (BFF) |
| Jobs | BackgroundService + PeriodicTimer (interface IScheduledJob) |
| E-mail | MailKit (SMTP) com templates HTML embarcados |
| Testes | xUnit, FluentAssertions, NSubstitute, NetArchTest |

## Estrutura de projetos

```
src/
  LicenciamentoSoftware.Domain/           Entidades, value objects, DomainException
  LicenciamentoSoftware.Application/      Handlers, Commands, interfaces de porta, Jobs
  LicenciamentoSoftware.Infrastructure/   Repositórios Dapper, DbUp, Email (MailKit), Jobs
  LicenciamentoSoftware.Api/              Controllers, Middleware, configuração DI
  LicenciamentoSoftware.Client/           Cliente HTTP compartilhado (Web + MAUI) — DTOs e services
  LicenciamentoSoftware.Web/              Blazor WASM — páginas, componentes, autenticação em memória
  LicenciamentoSoftware.Web.Server/       BFF — serve o WASM, proxy YARP para API, cookie HttpOnly
  LicenciamentoSoftware.Maui/             App Desktop/Mobile MAUI (em desenvolvimento)

tests/
  LicenciamentoSoftware.Domain.Tests/     Testes unitários de domínio
  LicenciamentoSoftware.Application.Tests/ Testes unitários de handlers, serviços e jobs
  LicenciamentoSoftware.IntegrationTests/ Testes de integração (requer PostgreSQL)
```

## Status das fases

| Fase | Descrição | Status |
|---|---|---|
| 1 | Fundação — projetos, build, arquitetura, docker-compose | ✅ Concluída |
| 2 | Domínio e schema — 11 entidades, DbUp, Dapper, V001 | ✅ Concluída |
| 3 | Identidade — JWT, 2FA TOTP, auditoria, V002 | ✅ Concluída |
| 4 | Segurança — token HMAC, anti-replay, rate limiting, V003 | ✅ Concluída |
| 5 | CRUDs de gestão — Cliente, Usuario, ClienteFinal, Aplicacao, TipoLicenca | ✅ Concluída |
| 6 | Emissão e gestão de licenças — emissão, operações manuais, histórico | ✅ Concluída |
| 7 | API de validação — login, heartbeat, logout, instalação | ✅ Concluída |
| 8 | Jobs agendados — sessões, expiração, renovação, rotação de tokens, e-mail | ✅ Concluída |
| 9 | Frontend Web — Blazor WASM + BFF, CRUD em modais, token HMAC inline | ✅ Concluída |
| 9.1 | Dashboard Web + instrumentação de métricas e alertas | 🔜 Próxima |
| 10 | MAUI Desktop + Mobile — Windows e Android | 🔜 Planejada |
| 11 | CI/CD e infraestrutura — pipeline completo, deploy VM | 🔜 Planejada |

**Testes:** 207 aprovados, 0 falhas.

## Como rodar localmente

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL acessível (WSL2, Docker ou Supabase)

### 1. Configurar variáveis de ambiente

```powershell
$env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5432;Database=licenciamento;Username=postgres;Password=postgres"
$env:JwtSettings__Secret = "sua-chave-secreta-com-pelo-menos-32-caracteres"
```

### 2. Iniciar o PostgreSQL

```powershell
# Via WSL2 (recomendado)
wsl sudo service postgresql start
.\scripts\start-wsl2.ps1

# Via Docker
.\scripts\start-docker.ps1
```

### 3. Configurar User Secrets da API

No Visual Studio: clique direito em `LicenciamentoSoftware.Api` → **Manage User Secrets**:

```json
{
  "JwtSettings:Secret": "sua-chave-secreta-minimo-32-caracteres",
  "ConnectionStrings:DefaultConnection": "Host=localhost;Port=5432;Database=licenciamento_dev;Username=postgres;Password=SUA-SENHA"
}
```

### 4. Rodar a API e o frontend

Configure **Multiple Startup Projects** no Visual Studio:

| Projeto | Action |
|---|---|
| `LicenciamentoSoftware.Api` | Start |
| `LicenciamentoSoftware.Web` | Start |
| `LicenciamentoSoftware.Web.Server` | Start |

Pressione **F5**. O browser abrirá automaticamente em `https://localhost:7152`.

As migrations do DbUp são aplicadas automaticamente na inicialização da API.

### 5. Só API (sem frontend)

```powershell
.\scripts\start-wsl2.ps1
```

A API fica disponível em `http://localhost:5016`.

### 6. Documentação interativa

```
https://localhost:7075/scalar/v1
```

### 7. Health check

```
GET https://localhost:7075/health
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

### Gestão (`/licencas`, `/clientes`, `/usuarios`, `/aplicacoes`, `/tipos-licenca`)

| Método | Rota | Descrição |
|---|---|---|
| GET/POST | `/licencas` | Lista e emite licenças |
| GET | `/licencas/{id}` | Detalhe de licença |
| DELETE | `/licencas/{id}` | Desativa licença |
| POST | `/licencas/{id}/token` | Emite token HMAC |
| POST | `/licencas/{id}/token/renovar` | Renova token HMAC |
| POST | `/licencas/{id}/renovar-periodo` | Renova período de licença |
| DELETE | `/licencas/{id}/sessoes/{idSessao}` | Encerra sessão manualmente |
| DELETE | `/licencas/{id}/instalacoes/{idInstalacao}` | Libera instalação manualmente |

### API de validação (`/api/validacao`)

> Autenticação via HMAC — headers `X-Token`, `X-Timestamp`, `X-Signature`, `X-Nonce`.

| Método | Rota | Descrição |
|---|---|---|
| POST | `/api/validacao/login` | Valida acesso (Permanente, Por Período, Por Usuários) |
| POST | `/api/validacao/heartbeat` | Keep-alive de sessão ativa |
| POST | `/api/validacao/logout` | Encerra sessão explicitamente (idempotente) |
| POST | `/api/validacao/instalacao` | Registra instalação em máquina (Por Instalação, idempotente) |

### Monitoramento

| Método | Rota | Descrição |
|---|---|---|
| GET | `/health` | Health check da API |
| GET | `/scalar/v1` | Documentação interativa (Development) |

## Autenticação HMAC para a API de validação

O software cliente deve incluir os seguintes headers em cada requisição:

```
X-Token:     <segredo-em-texto-puro>       (obtido na emissão do token, exibido uma única vez)
X-Timestamp: 2026-07-30T12:00:00Z         (ISO-8601 UTC, janela de ±5 min)
X-Nonce:     <uuid-ou-string-aleatória>   (máx 128 chars, único por requisição)
X-Signature: <hmac-sha256-hex>             (assinatura sobre idLicenca|X-Timestamp|body)
```

## Configuração de jobs e e-mail

Seções do `appsettings.json` (valores padrão):

```json
"JobSettings": {
  "DelayInicialSegundos": 30,
  "SessoesInativasIntervaloMinutos": 5,
  "SessoesInativasLimiteHoras": 24,
  "ExpiracaoLicencasIntervaloMinutos": 60,
  "RenovacaoAutomaticaIntervaloMinutos": 60,
  "RotacaoTokensIntervaloMinutos": 720,
  "NotificacaoIntervaloMinutos": 1440,
  "DiasAntecedenciaNotificacao": 7
},
"EmailSettings": {
  "Habilitado": false,
  "Host": "",
  "Porta": 587,
  "UsarSsl": false,
  "Usuario": "",
  "Senha": "",
  "EmailRemetente": "",
  "NomeRemetente": "LicenciamentoSoftware"
}
```

Para ativar o envio de e-mail, defina `Habilitado: true` e configure o SMTP via secrets ou variáveis de ambiente:

```powershell
$env:EmailSettings__Habilitado = "true"
$env:EmailSettings__Host = "smtp.seuprovedor.com"
$env:EmailSettings__Usuario = "seu@email.com"
$env:EmailSettings__Senha = "sua-senha-smtp"
$env:EmailSettings__EmailRemetente = "noreply@suaempresa.com"
```

## Frontend Web (Blazor WASM + BFF)

O portal web usa uma arquitetura BFF (Backend for Frontend) para segurança máxima:

```
Browser (Blazor WASM)
    ↕ HTTPS mesmo domínio
Web.Server (BFF — localhost:7152)
    ├── Serve os arquivos WASM estáticos
    ├── /bff/login → emite cookie HttpOnly com refresh token
    ├── /bff/refresh → renova access token via cookie
    ├── /bff/logout → revoga sessão
    └── Proxy YARP → repassa todas as chamadas da API com o Bearer token
    ↕ chamada interna
API (localhost:7075)
```

**Segurança dos tokens:**
- `AccessToken` (JWT) — fica exclusivamente em memória C# no WASM, nunca toca o DOM
- `RefreshToken` — fica no cookie `HttpOnly; Secure; SameSite=Strict`, invisível ao JavaScript

**Funcionalidades do portal:**
- Cadastro de empresa + login com 2FA TOTP
- CRUD de Clientes Finais, Usuários, Aplicações em modais inline
- Emissão de licenças por tipo (Permanente, Por Período, Por Usuários, Por Instalação)
- Gestão de sessões ativas, instalações registradas, operações manuais
- Geração e renovação de token HMAC com exibição única e botão Copiar

## Decisões de design

- **Sem EF Core** — DbUp para migrations, Dapper para queries
- **Rich Domain Model** — invariantes no domínio via `DomainException`
- **Sem MediatR** — handlers concretos injetados diretamente nos controllers
- **Tenant isolation** — `IdCliente` sempre do JWT, nunca do body
- **Segredo HMAC** — exibido uma única vez na emissão; apenas o hash BCrypt persiste
- **Jobs** — `IScheduledJob` + `BackgroundService`; migrável para Hangfire/Quartz sem impacto no domínio
- **Templates de e-mail** — arquivos HTML embarcados no assembly como `EmbeddedResource`

## Segurança em produção

- Use variáveis de ambiente ou secrets manager para `JwtSettings:Secret`, `ConnectionStrings__DefaultConnection`, `EmailSettings:Senha` e outros segredos
- Nunca commite segredos no repositório
- O `appsettings.json` contém apenas valores padrão vazios; o `appsettings.Development.json` é ignorado pelo `.gitignore`
