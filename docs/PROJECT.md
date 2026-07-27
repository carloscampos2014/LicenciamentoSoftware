# Sistema de Licenciamento de Software

## Visão Geral

Sistema responsável pelo gerenciamento de licenciamento de softwares.

O sistema permite que empresas cadastrem seus clientes, seus aplicativos e emitam licenças para utilização desses aplicativos. As aplicações licenciadas consultam uma **API de Validação de Licença** para verificar se o uso está autorizado.

## Objetivo

Fornecer uma plataforma centralizada para gerenciamento de:

- Clientes
- Usuários
- Clientes dos Clientes (Clientes Finais)
- Aplicações
- Tipos de Licença
- Licenças
- Log de Operações

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

Dados fixos (seed) — os valores de `Id` abaixo são ilustrativos, mas fixos e conhecidos pelo sistema (não gerados dinamicamente):

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

> O tipo de licença **não** é armazenado diretamente na Licença — é derivado através de `IdAplicativo` → `Aplicacao.IdTipoLicenca`, evitando redundância e possível inconsistência entre os dois campos.

#### Detalhamento por tipo de licença (tabelas específicas)

**LicencaPeriodo** (quando TipoLicenca = "Por Período")

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| LicencaId | Guid (FK) |
| DataInicio | Timestamp |
| DataFim | Timestamp |
| RenovacaoAutomatica | Bool |

- Quando `RenovacaoAutomatica = true`, um **job/rotina agendada** (execução diária) renova a vigência (estende `DataFim`) antes que a licença expire.
- Quando `RenovacaoAutomatica = false`, ao atingir `DataFim` a licença passa a ser considerada expirada e a API de validação nega o uso.

**LicencaUsuarios** (quando TipoLicenca = "Por Usuários")

Controla o número de **usuários simultâneos** (distintos) logados na aplicação, e quantas sessões cada usuário pode manter ao mesmo tempo.

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| LicencaId | Guid (FK) |
| QuantidadeMaxima | Int — máximo de usuários distintos logados simultaneamente |
| MaxSessoesPorUsuario | Int — máximo de sessões simultâneas por usuário (configurável por licença) |
| TempoLimiteSessaoHoras | Int — tempo máximo (em horas) sem heartbeat que uma sessão pode ficar antes de ser considerada inativa e encerrada automaticamente pelo job |

Não há cadastro prévio de usuário final — o identificador (username/e-mail) é apenas uma string livre enviada pelo software a cada login.

**LicencaSessao** (sessões ativas, usada para controle de "Por Usuários")

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| LicencaId | Guid (FK) |
| IdentificadorUsuario | Varchar(300) |
| DataLogin | Timestamp |
| DataUltimaAtividade | Timestamp — atualizada a cada heartbeat |
| Ativo | Bool |

- Cada login bem-sucedido gera um registro aqui. `Id` (SessaoId) é retornado ao software e usado depois na chamada de logout (ou heartbeat).
- Encerramento **normal** de sessão é explícito: o software chama o endpoint de logout informando o `SessaoId`.
- Enquanto a sessão estiver em uso, o software deve chamar periodicamente um endpoint de **heartbeat** (ex: a cada 5 minutos) informando o `SessaoId`, atualizando `DataUltimaAtividade`. Isso evita que uma sessão realmente ativa seja encerrada indevidamente pelo job de limpeza, e ao mesmo tempo garante que sessões abandonadas (sem logout) sejam liberadas com base em inatividade real — não em tempo total de login.
- Além do logout explícito (pelo software) e do job de limpeza por inatividade, o **Cliente** também pode encerrar uma sessão **manualmente** através do sistema de gestão (ex: quando o software travou e nem heartbeat nem logout ocorreram, e o Cliente não quer esperar o `TempoLimiteSessaoHoras`). Essa ação marca o registro em `LicencaSessao` como `Ativo = false`, liberando a vaga imediatamente.

**LicencaInstalacao** (quando TipoLicenca = "Por Instalação")

Controla o número de **máquinas distintas** autorizadas a executar a aplicação. Diferente da sessão de usuário, uma instalação não tem "logout" natural — ela permanece registrada até ser liberada manualmente.

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| LicencaId | Guid (FK) |
| QuantidadeMaxima | Int — máximo de máquinas distintas autorizadas |

**LicencaInstalacaoRegistrada** (máquinas já autorizadas)

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| LicencaId | Guid (FK) |
| IdentificadorMaquina | Varchar(300) — nome de máquina ou identificador equivalente enviado pelo software |
| DataRegistro | Timestamp |
| Ativo | Bool |

- A liberação de uma vaga (ex: Cliente Final trocou de máquina) é feita **manualmente**, pelo Cliente ou administrador, através do sistema de gestão — desativando o registro antigo. Não existe expiração automática, já que não há como o job inferir se uma máquina "parou de ser usada".

**Permanente** — sem tabela de detalhamento; a licença nunca expira e não possui limites de uso.

### Log de Operações

Tabela única e genérica, registrando operações realizadas em qualquer entidade do sistema:

| Campo | Tipo |
|--------|------|
| Id | Guid (PK) |
| Entidade | Varchar(100) |
| IdRegistro | Guid |
| Operacao | Char(1) — I/U/D (Insert/Update/Delete) |
| DataHora | Timestamp |
| IdUsuario | Guid (FK) |
| CamposAlterados | JSON/Text (nullable) |

- `CamposAlterados` armazena apenas o diff (campo, valor anterior, valor novo), não o registro completo antes/depois — mais leve e suficiente para auditoria/histórico.

### API de Validação de Licença

Endpoints consumidos pelos softwares licenciados para verificar se o uso está autorizado.

- **Identificação da licença:** feita pela combinação `IdCliente` + `IdClienteFinal` + `IdAplicativo` (sem chave/token separado).
- **Regras gerais:** a licença deve estar `Ativo = true` para qualquer validação.

#### `POST /validar-login` (relevante para licença Por Usuários)

Recebe: `IdCliente`, `IdClienteFinal`, `IdAplicativo`, `IdentificadorUsuario`.

Fluxo de verificação:
1. Localiza a licença ativa correspondente e seu tipo (via `IdAplicativo` → `Aplicacao.IdTipoLicenca`); se não for do tipo "Por Usuários", aplica a regra do tipo correspondente (Permanente / Por Período / Por Instalação).
2. Conta as sessões ativas (`LicencaSessao.Ativo = true`) desse `IdentificadorUsuario` para essa licença.
   - Se a contagem já atingiu `MaxSessoesPorUsuario` → **nega**, retornando mensagem informando que o **limite de sessões** para aquele usuário foi atingido.
3. Se for um `IdentificadorUsuario` novo (sem sessão ativa), conta quantos usuários **distintos** possuem sessão ativa na licença.
   - Se esse número já atingiu `QuantidadeMaxima` → **nega**, retornando mensagem informando que o **limite de usuários** da licença foi atingido.
4. Caso contrário, cria um novo registro em `LicencaSessao` (`Ativo = true`) e **libera** o acesso, retornando o `SessaoId` gerado.

#### `POST /logout`

Recebe: `SessaoId`.

Marca o registro correspondente em `LicencaSessao` como `Ativo = false` (encerra a sessão), liberando a vaga de usuário/sessão.

#### `POST /heartbeat`

Recebe: `SessaoId`.

Atualiza `DataUltimaAtividade` da sessão correspondente para o momento atual, confirmando que ela continua em uso real. O software deve chamar este endpoint periodicamente (ex: a cada 5 minutos) enquanto a sessão estiver ativa.

#### `POST /validar-instalacao` (relevante para licença Por Instalação)

Recebe: `IdCliente`, `IdClienteFinal`, `IdAplicativo`, `IdentificadorMaquina`.

Fluxo de verificação:
1. Localiza a licença ativa correspondente do tipo "Por Instalação".
2. Se o `IdentificadorMaquina` já possui registro ativo em `LicencaInstalacaoRegistrada` → **libera** (máquina já autorizada).
3. Se for uma máquina nova, conta quantas máquinas distintas já estão registradas e ativas.
   - Se essa contagem já atingiu `QuantidadeMaxima` → **nega**, retornando mensagem informando que o **limite de instalações** da licença foi atingido.
4. Caso contrário, cria um novo registro em `LicencaInstalacaoRegistrada` (`Ativo = true`) e **libera** o acesso.

#### Regras de validação por tipo

  - **Permanente:** sempre liberado (se ativa).
  - **Por Período:** liberado se `DataFim` ainda não foi atingida.
  - **Por Usuários:** liberado conforme fluxo de `/validar-login` acima.
  - **Por Instalação:** liberado conforme fluxo de `/validar-instalacao` acima.

Toda chamada à API (login, logout, validação) gera um registro no **Log de Operações** (ex: Entidade = "LicencaSessao").

### Rotina Agendada (Job)

Processo automático (ex: execução diária, ou com frequência maior para sessões) responsável por:
- Verificar licenças `Por Período` com `RenovacaoAutomatica = true` próximas do vencimento e renovar (estender `DataFim`).
- Marcar como expiradas/inativas as licenças `Por Período` com `RenovacaoAutomatica = false` que atingiram `DataFim`.
- Encerrar automaticamente (`Ativo = false`) sessões em `LicencaSessao` cuja `DataUltimaAtividade` (heartbeat) esteja há mais tempo que o `TempoLimiteSessaoHoras` da licença correspondente — ou seja, sem heartbeat recente, indicando inatividade real (ex: software fechou/travou sem chamar `/logout`). Isso evita liberar indevidamente vagas de sessões que continuam genuinamente em uso.

## Limitações e Premissas

- O controle de licenciamento (login, heartbeat, logout, limites de uso) depende da **integração correta do software do Cliente** com a API de Validação. O sistema aplica as regras a partir do que o software informa a cada chamada — não há como impedir, a partir do backend de licenciamento, que um software mal implementado (ou deliberadamente alterado) deixe de chamar a API corretamente.
- A responsabilidade por chamar a API de forma fiel (login a cada sessão, heartbeat periódico, logout ao encerrar) é do **Cliente** (dono do software), que é também o principal interessado em um controle rígido, já que é sua receita de licenciamento que está sendo protegida.
- Mecanismos de proteção contra engenharia reversa, adulteração de binário ou interceptação de chamadas (ex: ofuscação, assinatura de requisições, DRM) estão **fora do escopo** deste sistema, que é uma plataforma de gestão e validação de licenças — não uma solução de proteção de software.

## Regras de Negócio

- Todo usuário pertence a um cliente.
- Todo cliente final pertence a um cliente.
- Toda aplicação pertence a um cliente.
- Toda aplicação possui um tipo de licença.
- Uma licença vincula um cliente final a uma aplicação.
- Tipos de licença são fixos e globais (não pertencem a um cliente específico).
- Exclusão lógica através do campo Ativo.
- Toda operação relevante do sistema deve gerar um registro no Log de Operações.
- A validação de uso de uma aplicação licenciada é feita via API, identificando a licença pela combinação Cliente + Cliente Final + Aplicação.
