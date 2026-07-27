-- =========================================================
-- Sistema de Licenciamento de Software - Schema (PostgreSQL)
-- =========================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ---------------------------------------------------------
-- Cliente
-- ---------------------------------------------------------
CREATE TABLE Cliente (
    Id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    RazaoSocial     VARCHAR(200) NOT NULL,
    TipoInscricao   INTEGER NOT NULL,
    NumeroInscricao VARCHAR(20) NOT NULL,
    Email           VARCHAR(300) NOT NULL,
    Telefone        VARCHAR(15),
    Ativo           BOOLEAN NOT NULL DEFAULT TRUE
);

-- ---------------------------------------------------------
-- Usuario
-- ---------------------------------------------------------
CREATE TABLE Usuario (
    Id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    IdCliente   UUID NOT NULL REFERENCES Cliente(Id),
    Nome        VARCHAR(200) NOT NULL,
    Ativo       BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX idx_usuario_idcliente ON Usuario(IdCliente);

-- ---------------------------------------------------------
-- ClienteFinal
-- ---------------------------------------------------------
CREATE TABLE ClienteFinal (
    Id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    IdCliente       UUID NOT NULL REFERENCES Cliente(Id),
    RazaoSocial     VARCHAR(200) NOT NULL,
    TipoInscricao   INTEGER NOT NULL,
    NumeroInscricao VARCHAR(20) NOT NULL,
    Email           VARCHAR(300) NOT NULL,
    Telefone        VARCHAR(15),
    Ativo           BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX idx_clientefinal_idcliente ON ClienteFinal(IdCliente);

-- ---------------------------------------------------------
-- TipoLicenca (tabela fixa/global - seed)
-- ---------------------------------------------------------
CREATE TABLE TipoLicenca (
    Id          UUID PRIMARY KEY,
    Descricao   VARCHAR(200) NOT NULL
);

INSERT INTO TipoLicenca (Id, Descricao) VALUES
    ('11111111-1111-1111-1111-111111111111', 'Permanente'),
    ('22222222-2222-2222-2222-222222222222', 'Por Período'),
    ('33333333-3333-3333-3333-333333333333', 'Por Usuários'),
    ('44444444-4444-4444-4444-444444444444', 'Por Instalação');

-- ---------------------------------------------------------
-- Aplicacao
-- ---------------------------------------------------------
CREATE TABLE Aplicacao (
    Id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    IdCliente       UUID NOT NULL REFERENCES Cliente(Id),
    Titulo          VARCHAR(120) NOT NULL,
    Descricao       VARCHAR(300),
    IdTipoLicenca   UUID NOT NULL REFERENCES TipoLicenca(Id),
    Ativo           BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX idx_aplicacao_idcliente ON Aplicacao(IdCliente);
CREATE INDEX idx_aplicacao_idtipolicenca ON Aplicacao(IdTipoLicenca);

-- ---------------------------------------------------------
-- Licenca
-- Tipo de licença NÃO fica aqui: é derivado via
-- IdAplicativo -> Aplicacao.IdTipoLicenca
-- ---------------------------------------------------------
CREATE TABLE Licenca (
    Id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    IdCliente       UUID NOT NULL REFERENCES Cliente(Id),
    IdClienteFinal  UUID NOT NULL REFERENCES ClienteFinal(Id),
    IdAplicativo    UUID NOT NULL REFERENCES Aplicacao(Id),
    DataCadastro    TIMESTAMP NOT NULL DEFAULT now(),
    Ativo           BOOLEAN NOT NULL DEFAULT TRUE,

    -- Identificação usada pela API de validação: Cliente + ClienteFinal + Aplicativo.
    -- Garante que só exista uma licença ativa para essa combinação.
    CONSTRAINT uq_licenca_combinacao_ativa UNIQUE (IdCliente, IdClienteFinal, IdAplicativo, Ativo)
);

CREATE INDEX idx_licenca_idcliente ON Licenca(IdCliente);
CREATE INDEX idx_licenca_idclientefinal ON Licenca(IdClienteFinal);
CREATE INDEX idx_licenca_idaplicativo ON Licenca(IdAplicativo);
CREATE INDEX idx_licenca_lookup ON Licenca(IdCliente, IdClienteFinal, IdAplicativo) WHERE Ativo = TRUE;

-- ---------------------------------------------------------
-- LicencaPeriodo (detalhe - Tipo "Por Período")
-- ---------------------------------------------------------
CREATE TABLE LicencaPeriodo (
    Id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    LicencaId               UUID NOT NULL UNIQUE REFERENCES Licenca(Id),
    DataInicio              TIMESTAMP NOT NULL,
    DataFim                 TIMESTAMP NOT NULL,
    RenovacaoAutomatica     BOOLEAN NOT NULL DEFAULT FALSE
);

CREATE INDEX idx_licencaperiodo_datafim ON LicencaPeriodo(DataFim);

-- ---------------------------------------------------------
-- LicencaUsuarios (detalhe - Tipo "Por Usuários")
-- ---------------------------------------------------------
CREATE TABLE LicencaUsuarios (
    Id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    LicencaId               UUID NOT NULL UNIQUE REFERENCES Licenca(Id),
    QuantidadeMaxima        INTEGER NOT NULL,
    MaxSessoesPorUsuario    INTEGER NOT NULL DEFAULT 5,
    TempoLimiteSessaoHoras  INTEGER NOT NULL DEFAULT 24
);

-- ---------------------------------------------------------
-- LicencaSessao (sessões ativas - controle "Por Usuários")
-- ---------------------------------------------------------
CREATE TABLE LicencaSessao (
    Id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    LicencaId               UUID NOT NULL REFERENCES Licenca(Id),
    IdentificadorUsuario    VARCHAR(300) NOT NULL,
    DataLogin               TIMESTAMP NOT NULL DEFAULT now(),
    DataUltimaAtividade     TIMESTAMP NOT NULL DEFAULT now(),
    Ativo                   BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX idx_licencasessao_licenca_ativo ON LicencaSessao(LicencaId, Ativo);
CREATE INDEX idx_licencasessao_usuario ON LicencaSessao(LicencaId, IdentificadorUsuario, Ativo);
CREATE INDEX idx_licencasessao_ultimaatividade ON LicencaSessao(DataUltimaAtividade) WHERE Ativo = TRUE;

-- ---------------------------------------------------------
-- LicencaInstalacao (detalhe - Tipo "Por Instalação")
-- ---------------------------------------------------------
CREATE TABLE LicencaInstalacao (
    Id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    LicencaId           UUID NOT NULL UNIQUE REFERENCES Licenca(Id),
    QuantidadeMaxima    INTEGER NOT NULL
);

-- ---------------------------------------------------------
-- LicencaInstalacaoRegistrada (máquinas autorizadas)
-- ---------------------------------------------------------
CREATE TABLE LicencaInstalacaoRegistrada (
    Id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    LicencaId               UUID NOT NULL REFERENCES Licenca(Id),
    IdentificadorMaquina    VARCHAR(300) NOT NULL,
    DataRegistro            TIMESTAMP NOT NULL DEFAULT now(),
    Ativo                   BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE INDEX idx_licencainstalacao_licenca_ativo ON LicencaInstalacaoRegistrada(LicencaId, Ativo);
CREATE UNIQUE INDEX uq_licencainstalacao_maquina_ativa
    ON LicencaInstalacaoRegistrada(LicencaId, IdentificadorMaquina)
    WHERE Ativo = TRUE;

-- ---------------------------------------------------------
-- LogOperacao (log genérico de operações)
-- ---------------------------------------------------------
CREATE TABLE LogOperacao (
    Id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    Entidade        VARCHAR(100) NOT NULL,
    IdRegistro      UUID NOT NULL,
    Operacao        CHAR(1) NOT NULL CHECK (Operacao IN ('I', 'U', 'D')),
    DataHora        TIMESTAMP NOT NULL DEFAULT now(),
    IdUsuario       UUID REFERENCES Usuario(Id),
    CamposAlterados JSONB
);

CREATE INDEX idx_logoperacao_entidade_registro ON LogOperacao(Entidade, IdRegistro);
CREATE INDEX idx_logoperacao_datahora ON LogOperacao(DataHora);
