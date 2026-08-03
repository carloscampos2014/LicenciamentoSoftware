# Sistema de Licenciamento de Software

## Visão Geral

Sistema responsável pelo gerenciamento de licenciamento de softwares.

O sistema permite que empresas cadastrem seus clientes, seus aplicativos e emitam licenças para utilização desses aplicativos. As aplicações licenciadas consultam uma **API de Validação de Licença** para verificar se o uso está autorizado.

Os administradores acessam o sistema por três interfaces com paridade funcional: portal web (Blazor WASM), aplicativo desktop (MAUI Windows) e aplicativo mobile (MAUI Android). As três consomem a mesma API REST de gestão.

## Objetivo

Fornecer uma plataforma centralizada para gerenciamento de:

- Clientes
- Usuários
- Clientes dos Clientes (Clientes Finais)
- Aplicações
- Tipos de Licença
- Licenças
- Log de Operações

## Interfaces

| Interface | Tecnologia | Distribuição |
|---|---|---|
| Portal Web | Blazor WebAssembly + BFF | Oracle Cloud VM (Nginx) |
| Desktop | .NET MAUI (Windows) | Instalador direto |
| Mobile | .NET MAUI (Android) | Google Play Store |

Todas as interfaces consomem a mesma API REST de gestão e têm paridade funcional completa.

## Funcionalidades

### Cadastro de Clientes

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| RazaoSocial | Varchar(200) |
| TipoInscricao | Int |
| NumeroInscricao | Varchar(20) |
| Email | Varchar(300) |
| Telefone | Varchar(15) |
| Ativo | Bool |

> `TipoInscricao` padronizado como `Int` em Cliente e Cliente Final, indicando o tipo de inscrição (ex: CPF/CNPJ).

### Cadastro de Usuários

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| IdCliente | Guid (FK) |
| Nome | Varchar(200) |
| Ativo | Bool |

O primeiro usuário cadastrado para uma empresa recebe automaticamente o papel `AdministradorCliente`.

### Cadastro de Clientes Finais (Clientes dos Clientes)

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| IdCliente | Guid (FK) |
| RazaoSocial | Varchar(200) |
| TipoInscricao | Int |
| NumeroInscricao | Varchar(20) |
| Email | Varchar(300) |
| Telefone | Varchar(15) |
| Ativo | Bool |

### Cadastro de Aplicações

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| IdCliente | Guid (FK) |
| Titulo | Varchar(120) |
| Descricao | Varchar(300) |
| IdTipoLicenca | Guid (FK) |
| Ativo | Bool |

### Tipos de Licença (tabela fixa e global)

Não pertence a um Cliente específico — é uma tabela de domínio, compartilhada por todo o sistema, com dados pré-cadastrados (seed):

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| Descricao | Varchar(200) |

Dados fixos (seed):

| Id (Guid) | Descricao |
|-----------|-----------|
| 11111111-1111-1111-1111-111111111111 | Permanente |
| 22222222-2222-2222-2222-222222222222 | Por Período |
| 33333333-3333-3333-3333-333333333333 | Por Usuários |
| 44444444-4444-4444-4444-444444444444 | Por Instalação |

### Cadastro de Licenças

Tabela principal, comum a todos os tipos:

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| IdCliente | Guid (FK) |
| IdClienteFinal | Guid (FK) |
| IdAplicativo | Guid (FK) |
| DataCadastro | Timestamp |
| Ativo | Bool |

> O tipo de licença **não** é armazenado diretamente na Licença — é derivado através de `IdAplicativo` → `Aplicacao.IdTipoLicenca`, evitando redundância e inconsistência.

Na emissão da licença, um **token HMAC** é gerado e vinculado à licença. Esse token é usado pelo software licenciado para autenticar chamadas à API de validação. O token tem expiração automática e pode ser renovado manualmente pelo `AdministradorCliente`.

#### Detalhamento por tipo de licença (tabelas específicas)

**LicencaPeriodo** (quando TipoLicenca = "Por Período")

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| LicencaId | Guid (FK) |
| DataInicio | Timestamp |
| DataFim | Timestamp |
| RenovacaoAutomatica | Bool |

- Quando `RenovacaoAutomatica = true`, um job diário renova a vigência (estende `DataFim`) antes da expiração.
- Quando `RenovacaoAutomatica = false`, ao atingir `DataFim` a licença é considerada expirada e a API nega o uso.

**LicencaUsuarios** (quando TipoLicenca = "Por Usuários")

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| LicencaId | Guid (FK) |
| QuantidadeMaxima | Int — máximo de usuários distintos logados simultaneamente |
| MaxSessoesPorUsuario | Int — máximo de sessões simultâneas por usuário |
| TempoLimiteSessaoHoras | Int — tempo máximo sem heartbeat antes da sessão ser encerrada pelo job |

**LicencaSessao** (sessões ativas — controle "Por Usuários")

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| LicencaId | Guid (FK) |
| IdentificadorUsuario | Varchar(300) |
| DataLogin | Timestamp |
| DataUltimaAtividade | Timestamp — atualizada a cada heartbeat |
| Ativo | Bool |

- Cada login bem-sucedido gera um registro. O `SessaoId` é retornado ao software para uso em heartbeat e logout.
- O software deve chamar `/heartbeat` periodicamente (ex: a cada 5 minutos) para manter a sessão ativa.
- O `AdministradorCliente` pode encerrar sessões manualmente pelo portal (ex: quando o software travou).

**LicencaInstalacao** (quando TipoLicenca = "Por Instalação")

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| LicencaId | Guid (FK) |
| QuantidadeMaxima | Int — máximo de máquinas distintas autorizadas |

**LicencaInstalacaoRegistrada** (máquinas autorizadas)

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| LicencaId | Guid (FK) |
| IdentificadorMaquina | Varchar(300) |
| DataRegistro | Timestamp |
| Ativo | Bool |

- Liberação de vaga é feita manualmente pelo `AdministradorCliente` pelo portal — sem expiração automática.

**Permanente** — sem tabela de detalhamento; a licença nunca expira.

### Log de Operações

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| Entidade | Varchar(100) |
| IdRegistro | Guid |
| Operacao | Char(1) — I/U/D (Insert/Update/Delete) |
| DataHora | Timestamp |
| IdUsuario | Guid (FK) |
| CamposAlterados | JSON/Text (nullable) |

`CamposAlterados` armazena apenas o diff (campo, valor anterior, valor novo) — mais leve e suficiente para auditoria.

### API de Validação de Licença

Endpoints consumidos pelos softwares licenciados para verificar se o uso está autorizado.

**Autenticação:** cada requisição deve ser assinada com HMAC-SHA256 usando o token da licença e um timestamp. O servidor rejeita requisições com timestamp fora de ±5 minutos (proteção anti-replay).

- **Regras gerais:** a licença deve estar `Ativo = true` para qualquer validação.

#### `POST /validar-login` (Por Usuários)

Recebe: `IdCliente`, `IdClienteFinal`, `IdAplicativo`, `IdentificadorUsuario`.

Fluxo:
1. Localiza licença ativa e deriva tipo via `IdAplicativo → Aplicacao.IdTipoLicenca`.
2. Conta sessões ativas do `IdentificadorUsuario` para essa licença.
   - Se atingiu `MaxSessoesPorUsuario` → **nega** (limite de sessões do usuário atingido).
3. Se usuário novo, conta usuários distintos com sessão ativa.
   - Se atingiu `QuantidadeMaxima` → **nega** (limite de usuários da licença atingido).
4. Cria registro em `LicencaSessao` e **libera**, retornando `SessaoId`.

#### `POST /logout`

Recebe: `SessaoId`. Marca sessão como `Ativo = false`, liberando a vaga.

#### `POST /heartbeat`

Recebe: `SessaoId`. Atualiza `DataUltimaAtividade` para o momento atual.

#### `POST /validar-instalacao` (Por Instalação)

Recebe: `IdCliente`, `IdClienteFinal`, `IdAplicativo`, `IdentificadorMaquina`.

Fluxo:
1. Localiza licença ativa do tipo "Por Instalação".
2. Se máquina já registrada e ativa → **libera** (idempotente).
3. Se máquina nova, conta máquinas registradas e ativas.
   - Se atingiu `QuantidadeMaxima` → **nega** (limite de instalações atingido).
4. Registra máquina e **libera**.

#### Regras por tipo

| Tipo | Regra de validação |
|---|---|
| Permanente | Sempre libera se licença ativa |
| Por Período | Libera se `DataFim` não foi atingida |
| Por Usuários | Fluxo de `/validar-login` |
| Por Instalação | Fluxo de `/validar-instalacao` |

### Rotina Agendada (Job)

- Encerrar sessões sem heartbeat além de `TempoLimiteSessaoHoras`.
- Expirar licenças Por Período vencidas com `RenovacaoAutomatica = false`.
- Renovar `DataFim` de licenças Por Período com `RenovacaoAutomatica = true`.
- Registrar log de tokens de licença próximos do vencimento.

## Segurança

### Portal de gestão (Web, Desktop, Mobile)

- Autenticação JWT com refresh token rotacionável.
- 2FA obrigatório via TOTP (Google Authenticator / Authy).
- Papéis: `AdministradorPlataforma`, `AdministradorCliente`, `OperadorCliente`, `Leitor`.
- Tenant sempre derivado da identidade autenticada — nunca do body da requisição.

### API de validação

- Token por licença com expiração automática, armazenado como hash.
- Autenticação por assinatura HMAC-SHA256 com timestamp (proteção anti-replay).
- Renovação manual pelo `AdministradorCliente`; token anterior invalidado imediatamente.
- Rate limiting por IP e por token.

## Limitações e Premissas

- O controle de licenciamento depende da integração correta do software do Cliente com a API. O sistema aplica as regras a partir do que o software informa — não há como impedir um software mal implementado de não chamar a API corretamente.
- A responsabilidade pela integração fiel (login, heartbeat, logout) é do Cliente, que é o principal interessado no controle rígido.
- Mecanismos de proteção contra engenharia reversa, ofuscação ou DRM estão fora do escopo deste sistema.

## Regras de Negócio

- Todo usuário pertence a um cliente.
- Todo cliente final pertence a um cliente.
- Toda aplicação pertence a um cliente.
- Toda aplicação possui um tipo de licença.
- Uma licença vincula um cliente final a uma aplicação.
- Tipos de licença são fixos e globais.
- Exclusão sempre lógica (`Ativo = false`).
- Toda operação relevante gera registro no Log de Operações.
- A validação é feita via API, identificando a licença pela combinação Cliente + Cliente Final + Aplicação, autenticada por HMAC.
