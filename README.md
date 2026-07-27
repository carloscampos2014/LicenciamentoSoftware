# LicenciamentoSoftware

Estrutura .NET 8 (Clean Architecture) para o Sistema de Licenciamento de Software, gerada a partir do `PROJECT.md` / `schema.sql`.

## Estrutura

```
src/
  LicenciamentoSoftware.Domain/          Entidades de domínio (POCOs)
  LicenciamentoSoftware.Infrastructure/  DbContext (EF Core) + mapeamento Fluent API
  LicenciamentoSoftware.Api/             API (Controllers, DTOs, Services)
```

## O que já está implementado

- Todas as entidades do modelo (Cliente, Usuario, ClienteFinal, Aplicacao, TipoLicenca,
  Licenca, LicencaPeriodo, LicencaUsuarios, LicencaSessao, LicencaInstalacao,
  LicencaInstalacaoRegistrada, LogOperacao).
- `LicenciamentoDbContext` com o mapeamento completo (chaves, índices, unique constraints,
  seed dos Tipos de Licença fixos).
- API de Validação de Licença completa:
  - `POST /api/validar-login` — licença Por Usuários (limite de usuários simultâneos + limite de sessões por usuário)
  - `POST /api/heartbeat` — mantém sessão viva
  - `POST /api/logout` — encerra sessão explicitamente
  - `POST /api/validar-instalacao` — licença Por Instalação (limite de máquinas distintas)

## CRUDs implementados

- `ClientesController` — `GET /api/clientes`, `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` (desativação lógica).
- `UsuariosController` — idem, com filtro por `idCliente` e validação de que o Cliente existe/está ativo.
- `ClientesFinaisController` — idem, valida vínculo com Cliente.
- `AplicacoesController` — idem, valida Cliente e `IdTipoLicenca`.
- `TiposLicencaController` — somente leitura (`GET`), já que é tabela fixa/seed.
- `LicencasController` — o mais complexo: valida que Cliente Final e Aplicativo pertencem ao Cliente informado, valida que o bloco de detalhe enviado (`Periodo` / `Usuarios` / `Instalacao`) corresponde ao `IdTipoLicenca` da Aplicação, cria o registro de detalhe correto, e trata a violação da constraint de licença ativa única (retorna `409 Conflict`).

Todos os CRUDs usam exclusão lógica (`Ativo = false`), nunca `DELETE` físico — consistente com a regra de negócio documentada.

## O que ainda falta (próximos passos sugeridos)

- Job agendado (`IHostedService` / Hangfire / Quartz) para: renovação automática de licenças Por Período, e liberação de sessões inativas (sem heartbeat dentro de `TempoLimiteSessaoHoras`).
- Gravação automática no `LogOperacao` a cada operação de escrita (ex: via `SaveChangesAsync` override no DbContext, ou interceptor do EF Core) — hoje os CRUDs alteram dados mas não geram log ainda.
- Autenticação/autorização da própria API de gestão (distinta da API de validação usada pelos softwares licenciados) — hoje qualquer chamada é aceita sem identidade.
- Validações de negócio adicionais (ex: paginação nas listagens, validação de formato de e-mail/CPF-CNPJ, tratamento de erros mais granular).
- Migrations do EF Core (`dotnet ef migrations add Inicial`) — não geradas aqui pois o ambiente não possui o SDK do .NET instalado para validar o build.
- Testes automatizados (unitários e de integração).

## Como rodar (ambiente com .NET 8 SDK instalado)

```bash
cd src/LicenciamentoSoftware.Api
dotnet restore
dotnet ef migrations add Inicial --project ../LicenciamentoSoftware.Infrastructure
dotnet ef database update
dotnet run
```

Ajuste a connection string em `appsettings.json` (`ConnectionStrings:DefaultConnection`) para seu PostgreSQL local.

> ⚠️ Este código não foi compilado/testado neste ambiente (sem SDK do .NET disponível). Revise ao rodar `dotnet build` pela primeira vez.
