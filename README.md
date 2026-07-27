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
- Testes automatizados (unitários e de integração).

## Como rodar (ambiente com .NET 8 SDK instalado)

```bash
cd src/LicenciamentoSoftware.Api
dotnet restore
# Caso precise gerar migrations localmente (já foi criada a migration inicial neste repositório):
# dotnet ef migrations add Inicial --project ../LicenciamentoSoftware.Infrastructure
# Aplicar migrations ao banco configurado em appsettings.json
dotnet ef database update --project ../LicenciamentoSoftware.Infrastructure --startup-project .
dotnet run
```

Ajuste a connection string em `appsettings.json` (`ConnectionStrings:DefaultConnection`) para seu PostgreSQL local.

## Docker (recomendado)

Modo rápido para levantar um PostgreSQL via Docker e aplicar as migrations:

1) Rodar container PostgreSQL (PowerShell):

```powershell
docker pull postgres:15
docker run --name lic_pg \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=licenciamento \
  -p 5432:5432 -d postgres:15
```

2) Aplicar migrations a partir da raiz do projeto (PowerShell):

```powershell
dotnet ef database update --project src\\LicenciamentoSoftware.Infrastructure --startup-project src\\LicenciamentoSoftware.Api
```

3) Alternativa: executar o script SQL já gerado (se preferir não usar dotnet-ef no host):

```powershell
docker cp .\\src\\Database\\create_schema.sql lic_pg:/tmp/create_schema.sql
docker exec -it lic_pg psql -U postgres -d licenciamento -f /tmp/create_schema.sql
```

4) Parar/remover o container quando não precisar:

```powershell
docker stop lic_pg; docker rm lic_pg
```

## WSL2 / PostgreSQL no Linux (Ubuntu)

Passos resumidos (execute dentro do WSL):

```bash
sudo apt update
sudo apt install -y postgresql postgresql-contrib
sudo service postgresql start
# Ajuste a senha do usuário postgres (opcional)
sudo -u postgres psql -c "ALTER USER postgres WITH PASSWORD 'postgres';"
sudo -u postgres createdb licenciamento
# Executar o script SQL gerado (caminho para o Windows: /mnt/c/...)
sudo -u postgres psql -d licenciamento -f /mnt/c/Dev/LicenciamentoSoftware/src/Database/create_schema.sql
```

Ou aplique as migrations diretamente (no WSL ou no Windows se o SDK estiver instalado):

```bash
dotnet ef database update --project src/LicenciamentoSoftware.Infrastructure --startup-project src/LicenciamentoSoftware.Api
```

## dotnet-ef (ferramenta global)

Instalar/atualizar a ferramenta para evitar mensagens de mismatch:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.10
# ou
dotnet tool update --global dotnet-ef --version 8.0.10
```

## Arquivos importantes

- Migrations: `src/LicenciamentoSoftware.Infrastructure/Migrations/InitialCreate*`
- Script SQL gerado: `src/Database/create_schema.sql`
- Connection string padrão: `src/LicenciamentoSoftware.Api/appsettings.json` (Host=localhost;Port=5432;Database=licenciamento;Username=postgres;Password=postgres)

## Observações

- Em produção, não use as credenciais padrão e configure backups e políticas de segurança.
- Se alterar a connection string, atualize `appsettings.json` ou use variáveis de ambiente.

---

Foi adicionado um script PowerShell em `scripts/start-local.ps1` que automatiza a criação (ou reutilização) do container Docker PostgreSQL, aplica as migrations via EF Core e inicia a API.

Uso (PowerShell, execute na raiz do repositório):

```powershell
# Criar/usar container, aplicar migrations e iniciar a API
.\scripts\start-local.ps1

# Forçar recriar o container PostgreSQL antes de aplicar migrations
.\scripts\start-local.ps1 -RecreateContainer
```

Se desejar, posso também adicionar scripts para parar/remover o ambiente, executar a API em background ou parametrizar usuário/senha/porta via arquivo `.env`.
