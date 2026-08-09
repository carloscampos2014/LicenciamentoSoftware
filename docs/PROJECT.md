# Sistema de Licenciamento de Software

## VisÃ£o Geral

Sistema responsÃ¡vel pelo gerenciamento de licenciamento de softwares.

O sistema permite que empresas cadastrem seus clientes, seus aplicativos e emitam licenÃ§as para utilizaÃ§Ã£o desses aplicativos. As aplicaÃ§Ãµes licenciadas consultam uma **API de ValidaÃ§Ã£o de LicenÃ§a** para verificar se o uso estÃ¡ autorizado.

Os administradores acessam o sistema por trÃªs interfaces com paridade funcional: portal web (Blazor WASM), aplicativo desktop (MAUI Windows) e aplicativo mobile (MAUI Android). As trÃªs consomem a mesma API REST de gestÃ£o.

## Objetivo

Fornecer uma plataforma centralizada para gerenciamento de:

- Clientes
- UsuÃ¡rios
- Clientes dos Clientes (Clientes Finais)
- AplicaÃ§Ãµes
- Tipos de LicenÃ§a
- LicenÃ§as
- Log de OperaÃ§Ãµes

## Interfaces

| Interface | Tecnologia | DistribuiÃ§Ã£o |
|---|---|---|
| Portal Web | Blazor WebAssembly + BFF | Oracle Cloud VM (Nginx) |
| Desktop | .NET MAUI (Windows) | Instalador direto |
| Mobile | .NET MAUI (Android) | APK sideload (distribuiÃ§Ã£o direta, sem Google Play Store) |

Todas as interfaces consomem a mesma API REST de gestÃ£o e tÃªm paridade funcional completa.

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

> `TipoInscricao` padronizado como `Int` em Cliente e Cliente Final, indicando o tipo de inscriÃ§Ã£o (ex: CPF/CNPJ).

### Cadastro de UsuÃ¡rios

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| IdCliente | Guid (FK) |
| Nome | Varchar(200) |
| Ativo | Bool |

O primeiro usuÃ¡rio cadastrado para uma empresa recebe automaticamente o papel `AdministradorCliente`.

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

### Cadastro de AplicaÃ§Ãµes

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| IdCliente | Guid (FK) |
| Titulo | Varchar(120) |
| Descricao | Varchar(300) |
| IdTipoLicenca | Guid (FK) |
| Ativo | Bool |

### Tipos de LicenÃ§a (tabela fixa e global)

NÃ£o pertence a um Cliente especÃ­fico â€” Ã© uma tabela de domÃ­nio, compartilhada por todo o sistema, com dados prÃ©-cadastrados (seed):

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| Descricao | Varchar(200) |

Dados fixos (seed):

| Id (Guid) | Descricao |
|-----------|-----------|
| 11111111-1111-1111-1111-111111111111 | Permanente |
| 22222222-2222-2222-2222-222222222222 | Por PerÃ­odo |
| 33333333-3333-3333-3333-333333333333 | Por UsuÃ¡rios |
| 44444444-4444-4444-4444-444444444444 | Por InstalaÃ§Ã£o |

### Cadastro de LicenÃ§as

Tabela principal, comum a todos os tipos:

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| IdCliente | Guid (FK) |
| IdClienteFinal | Guid (FK) |
| IdAplicativo | Guid (FK) |
| DataCadastro | Timestamp |
| Ativo | Bool |

> O tipo de licenÃ§a **nÃ£o** Ã© armazenado diretamente na LicenÃ§a â€” Ã© derivado atravÃ©s de `IdAplicativo` â†’ `Aplicacao.IdTipoLicenca`, evitando redundÃ¢ncia e inconsistÃªncia.

Na emissÃ£o da licenÃ§a, um **token HMAC** Ã© gerado e vinculado Ã  licenÃ§a. Esse token Ã© usado pelo software licenciado para autenticar chamadas Ã  API de validaÃ§Ã£o. O token tem expiraÃ§Ã£o automÃ¡tica e pode ser renovado manualmente pelo `AdministradorCliente`.

#### Detalhamento por tipo de licenÃ§a (tabelas especÃ­ficas)

**LicencaPeriodo** (quando TipoLicenca = "Por PerÃ­odo")

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| LicencaId | Guid (FK) |
| DataInicio | Timestamp |
| DataFim | Timestamp |
| RenovacaoAutomatica | Bool |

- Quando `RenovacaoAutomatica = true`, um job diÃ¡rio renova a vigÃªncia (estende `DataFim`) antes da expiraÃ§Ã£o.
- Quando `RenovacaoAutomatica = false`, ao atingir `DataFim` a licenÃ§a Ã© considerada expirada e a API nega o uso.

**LicencaUsuarios** (quando TipoLicenca = "Por UsuÃ¡rios")

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| LicencaId | Guid (FK) |
| QuantidadeMaxima | Int â€” mÃ¡ximo de usuÃ¡rios distintos logados simultaneamente |
| MaxSessoesPorUsuario | Int â€” mÃ¡ximo de sessÃµes simultÃ¢neas por usuÃ¡rio |
| TempoLimiteSessaoHoras | Int â€” tempo mÃ¡ximo sem heartbeat antes da sessÃ£o ser encerrada pelo job |

**LicencaSessao** (sessÃµes ativas â€” controle "Por UsuÃ¡rios")

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| LicencaId | Guid (FK) |
| IdentificadorUsuario | Varchar(300) |
| DataLogin | Timestamp |
| DataUltimaAtividade | Timestamp â€” atualizada a cada heartbeat |
| Ativo | Bool |

- Cada login bem-sucedido gera um registro. O `SessaoId` Ã© retornado ao software para uso em heartbeat e logout.
- O software deve chamar `/heartbeat` periodicamente (ex: a cada 5 minutos) para manter a sessÃ£o ativa.
- O `AdministradorCliente` pode encerrar sessÃµes manualmente pelo portal (ex: quando o software travou).

**LicencaInstalacao** (quando TipoLicenca = "Por InstalaÃ§Ã£o")

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| LicencaId | Guid (FK) |
| QuantidadeMaxima | Int â€” mÃ¡ximo de mÃ¡quinas distintas autorizadas |

**LicencaInstalacaoRegistrada** (mÃ¡quinas autorizadas)

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| LicencaId | Guid (FK) |
| IdentificadorMaquina | Varchar(300) |
| DataRegistro | Timestamp |
| Ativo | Bool |

- LiberaÃ§Ã£o de vaga Ã© feita manualmente pelo `AdministradorCliente` pelo portal â€” sem expiraÃ§Ã£o automÃ¡tica.

**Permanente** â€” sem tabela de detalhamento; a licenÃ§a nunca expira.

### Log de OperaÃ§Ãµes

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| Entidade | Varchar(100) |
| IdRegistro | Guid |
| Operacao | Char(1) â€” I/U/D (Insert/Update/Delete) |
| DataHora | Timestamp |
| IdUsuario | Guid (FK) |
| CamposAlterados | JSON/Text (nullable) |

`CamposAlterados` armazena apenas o diff (campo, valor anterior, valor novo) â€” mais leve e suficiente para auditoria.

### API de ValidaÃ§Ã£o de LicenÃ§a

Endpoints consumidos pelos softwares licenciados para verificar se o uso estÃ¡ autorizado.

**AutenticaÃ§Ã£o:** cada requisiÃ§Ã£o deve ser assinada com HMAC-SHA256 usando o token da licenÃ§a e um timestamp. O servidor rejeita requisiÃ§Ãµes com timestamp fora de Â±5 minutos (proteÃ§Ã£o anti-replay).

- **Regras gerais:** a licenÃ§a deve estar `Ativo = true` para qualquer validaÃ§Ã£o.

#### `POST /validar-login` (Por UsuÃ¡rios)

Recebe: `IdCliente`, `IdClienteFinal`, `IdAplicativo`, `IdentificadorUsuario`.

Fluxo:
1. Localiza licenÃ§a ativa e deriva tipo via `IdAplicativo â†’ Aplicacao.IdTipoLicenca`.
2. Conta sessÃµes ativas do `IdentificadorUsuario` para essa licenÃ§a.
   - Se atingiu `MaxSessoesPorUsuario` â†’ **nega** (limite de sessÃµes do usuÃ¡rio atingido).
3. Se usuÃ¡rio novo, conta usuÃ¡rios distintos com sessÃ£o ativa.
   - Se atingiu `QuantidadeMaxima` â†’ **nega** (limite de usuÃ¡rios da licenÃ§a atingido).
4. Cria registro em `LicencaSessao` e **libera**, retornando `SessaoId`.

#### `POST /logout`

Recebe: `SessaoId`. Marca sessÃ£o como `Ativo = false`, liberando a vaga.

#### `POST /heartbeat`

Recebe: `SessaoId`. Atualiza `DataUltimaAtividade` para o momento atual.

#### `POST /validar-instalacao` (Por InstalaÃ§Ã£o)

Recebe: `IdCliente`, `IdClienteFinal`, `IdAplicativo`, `IdentificadorMaquina`.

Fluxo:
1. Localiza licenÃ§a ativa do tipo "Por InstalaÃ§Ã£o".
2. Se mÃ¡quina jÃ¡ registrada e ativa â†’ **libera** (idempotente).
3. Se mÃ¡quina nova, conta mÃ¡quinas registradas e ativas.
   - Se atingiu `QuantidadeMaxima` â†’ **nega** (limite de instalaÃ§Ãµes atingido).
4. Registra mÃ¡quina e **libera**.

#### Regras por tipo

| Tipo | Regra de validaÃ§Ã£o |
|---|---|
| Permanente | Sempre libera se licenÃ§a ativa |
| Por PerÃ­odo | Libera se `DataFim` nÃ£o foi atingida |
| Por UsuÃ¡rios | Fluxo de `/validar-login` |
| Por InstalaÃ§Ã£o | Fluxo de `/validar-instalacao` |

### Rotina Agendada (Job)

- Encerrar sessÃµes sem heartbeat alÃ©m de `TempoLimiteSessaoHoras`.
- Expirar licenÃ§as Por PerÃ­odo vencidas com `RenovacaoAutomatica = false`.
- Renovar `DataFim` de licenÃ§as Por PerÃ­odo com `RenovacaoAutomatica = true`.
- Registrar log de tokens de licenÃ§a prÃ³ximos do vencimento.

## SeguranÃ§a

### Portal de gestÃ£o (Web, Desktop, Mobile)

- AutenticaÃ§Ã£o JWT com refresh token rotacionÃ¡vel.
- 2FA obrigatÃ³rio via TOTP (Google Authenticator / Authy).
- PapÃ©is: `AdministradorPlataforma`, `AdministradorCliente`, `OperadorCliente`, `Leitor`.
- Tenant sempre derivado da identidade autenticada â€” nunca do body da requisiÃ§Ã£o.

### API de validaÃ§Ã£o

- Token por licenÃ§a com expiraÃ§Ã£o automÃ¡tica, armazenado como hash.
- AutenticaÃ§Ã£o por assinatura HMAC-SHA256 com timestamp (proteÃ§Ã£o anti-replay).
- RenovaÃ§Ã£o manual pelo `AdministradorCliente`; token anterior invalidado imediatamente.
- Rate limiting por IP e por token.

## LimitaÃ§Ãµes e Premissas

- O controle de licenciamento depende da integraÃ§Ã£o correta do software do Cliente com a API. O sistema aplica as regras a partir do que o software informa â€” nÃ£o hÃ¡ como impedir um software mal implementado de nÃ£o chamar a API corretamente.
- A responsabilidade pela integraÃ§Ã£o fiel (login, heartbeat, logout) Ã© do Cliente, que Ã© o principal interessado no controle rÃ­gido.
- Mecanismos de proteÃ§Ã£o contra engenharia reversa, ofuscaÃ§Ã£o ou DRM estÃ£o fora do escopo deste sistema.

## Regras de NegÃ³cio

- Todo usuÃ¡rio pertence a um cliente.
- Todo cliente final pertence a um cliente.
- Toda aplicaÃ§Ã£o pertence a um cliente.
- Toda aplicaÃ§Ã£o possui um tipo de licenÃ§a.
- Uma licenÃ§a vincula um cliente final a uma aplicaÃ§Ã£o.
- Tipos de licenÃ§a sÃ£o fixos e globais.
- ExclusÃ£o sempre lÃ³gica (`Ativo = false`).
- Toda operaÃ§Ã£o relevante gera registro no Log de OperaÃ§Ãµes.
- A validaÃ§Ã£o Ã© feita via API, identificando a licenÃ§a pela combinaÃ§Ã£o Cliente + Cliente Final + AplicaÃ§Ã£o, autenticada por HMAC.
