-- =========================================================
-- V001 - Schema inicial do Sistema de Licenciamento
-- Gerado a partir do modelo de domínio da Fase 2
-- =========================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ---------------------------------------------------------
-- Cliente
-- ---------------------------------------------------------
CREATE TABLE cliente (
    id               UUID        PRIMARY KEY DEFAULT uuid_generate_v4(),
    razao_social     VARCHAR(200) NOT NULL,
    tipo_inscricao   INTEGER      NOT NULL,
    numero_inscricao VARCHAR(20)  NOT NULL,
    email            VARCHAR(300) NOT NULL,
    telefone         VARCHAR(15),
    ativo            BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE UNIQUE INDEX uq_cliente_inscricao ON cliente(tipo_inscricao, numero_inscricao);

-- ---------------------------------------------------------
-- Usuario
-- ---------------------------------------------------------
CREATE TABLE usuario (
    id               UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    id_cliente       UUID         NOT NULL REFERENCES cliente(id),
    nome             VARCHAR(200) NOT NULL,
    senha_hash       VARCHAR(300) NOT NULL,
    totp_secret_hash VARCHAR(300) NULL,
    ativo            BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE INDEX idx_usuario_id_cliente ON usuario(id_cliente);

-- ---------------------------------------------------------
-- ClienteFinal
-- ---------------------------------------------------------
CREATE TABLE cliente_final (
    id               UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    id_cliente       UUID         NOT NULL REFERENCES cliente(id),
    razao_social     VARCHAR(200) NOT NULL,
    tipo_inscricao   INTEGER      NOT NULL,
    numero_inscricao VARCHAR(20)  NOT NULL,
    email            VARCHAR(300) NOT NULL,
    telefone         VARCHAR(15),
    ativo            BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE INDEX idx_cliente_final_id_cliente ON cliente_final(id_cliente);

-- ---------------------------------------------------------
-- TipoLicenca (tabela fixa/global — seed incluso)
-- ---------------------------------------------------------
CREATE TABLE tipo_licenca (
    id        UUID         PRIMARY KEY,
    descricao VARCHAR(200) NOT NULL
);

INSERT INTO tipo_licenca (id, descricao) VALUES
    ('11111111-1111-1111-1111-111111111111', 'Permanente'),
    ('22222222-2222-2222-2222-222222222222', 'Por Período'),
    ('33333333-3333-3333-3333-333333333333', 'Por Usuários'),
    ('44444444-4444-4444-4444-444444444444', 'Por Instalação');

-- ---------------------------------------------------------
-- Aplicacao
-- ---------------------------------------------------------
CREATE TABLE aplicacao (
    id              UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    id_cliente      UUID         NOT NULL REFERENCES cliente(id),
    titulo          VARCHAR(120) NOT NULL,
    descricao       VARCHAR(300),
    id_tipo_licenca UUID         NOT NULL REFERENCES tipo_licenca(id),
    ativo           BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE INDEX idx_aplicacao_id_cliente      ON aplicacao(id_cliente);
CREATE INDEX idx_aplicacao_id_tipo_licenca ON aplicacao(id_tipo_licenca);

-- ---------------------------------------------------------
-- Licenca
-- ---------------------------------------------------------
CREATE TABLE licenca (
    id               UUID      PRIMARY KEY DEFAULT uuid_generate_v4(),
    id_cliente       UUID      NOT NULL REFERENCES cliente(id),
    id_cliente_final UUID      NOT NULL REFERENCES cliente_final(id),
    id_aplicativo    UUID      NOT NULL REFERENCES aplicacao(id),
    data_cadastro    TIMESTAMP NOT NULL DEFAULT now(),
    ativo            BOOLEAN   NOT NULL DEFAULT TRUE
);

CREATE INDEX idx_licenca_id_cliente       ON licenca(id_cliente);
CREATE INDEX idx_licenca_id_cliente_final ON licenca(id_cliente_final);
CREATE INDEX idx_licenca_id_aplicativo    ON licenca(id_aplicativo);
CREATE INDEX idx_licenca_lookup           ON licenca(id_cliente, id_cliente_final, id_aplicativo) WHERE ativo = TRUE;

-- Garante apenas uma licença ativa por combinação cliente + cliente_final + aplicativo
CREATE UNIQUE INDEX uq_licenca_combinacao_ativa
    ON licenca(id_cliente, id_cliente_final, id_aplicativo)
    WHERE ativo = TRUE;

-- ---------------------------------------------------------
-- LicencaPeriodo (detalhe — tipo "Por Período")
-- ---------------------------------------------------------
CREATE TABLE licenca_periodo (
    id                   UUID      PRIMARY KEY DEFAULT uuid_generate_v4(),
    licenca_id           UUID      NOT NULL UNIQUE REFERENCES licenca(id),
    data_inicio          TIMESTAMP NOT NULL,
    data_fim             TIMESTAMP NOT NULL,
    renovacao_automatica BOOLEAN   NOT NULL DEFAULT FALSE
);

CREATE INDEX idx_licenca_periodo_data_fim ON licenca_periodo(data_fim);

-- ---------------------------------------------------------
-- LicencaUsuarios (detalhe — tipo "Por Usuários")
-- ---------------------------------------------------------
CREATE TABLE licenca_usuarios (
    id                       UUID    PRIMARY KEY DEFAULT uuid_generate_v4(),
    licenca_id               UUID    NOT NULL UNIQUE REFERENCES licenca(id),
    quantidade_maxima        INTEGER NOT NULL,
    max_sessoes_por_usuario  INTEGER NOT NULL DEFAULT 5,
    tempo_limite_sessao_horas INTEGER NOT NULL DEFAULT 24
);

-- ---------------------------------------------------------
-- LicencaSessao (sessões ativas — controle "Por Usuários")
-- ---------------------------------------------------------
CREATE TABLE licenca_sessao (
    id                     UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    licenca_id             UUID         NOT NULL REFERENCES licenca(id),
    identificador_usuario  VARCHAR(300) NOT NULL,
    data_login             TIMESTAMP    NOT NULL DEFAULT now(),
    data_ultima_atividade  TIMESTAMP    NOT NULL DEFAULT now(),
    ativo                  BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE INDEX idx_licenca_sessao_licenca_ativo ON licenca_sessao(licenca_id, ativo);
CREATE INDEX idx_licenca_sessao_usuario       ON licenca_sessao(licenca_id, identificador_usuario, ativo);
CREATE INDEX idx_licenca_sessao_ultima_atv    ON licenca_sessao(data_ultima_atividade) WHERE ativo = TRUE;

-- ---------------------------------------------------------
-- LicencaInstalacao (detalhe — tipo "Por Instalação")
-- ---------------------------------------------------------
CREATE TABLE licenca_instalacao (
    id                UUID    PRIMARY KEY DEFAULT uuid_generate_v4(),
    licenca_id        UUID    NOT NULL UNIQUE REFERENCES licenca(id),
    quantidade_maxima INTEGER NOT NULL
);

-- ---------------------------------------------------------
-- LicencaInstalacaoRegistrada (máquinas autorizadas)
-- ---------------------------------------------------------
CREATE TABLE licenca_instalacao_registrada (
    id                     UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    licenca_id             UUID         NOT NULL REFERENCES licenca(id),
    identificador_maquina  VARCHAR(300) NOT NULL,
    data_registro          TIMESTAMP    NOT NULL DEFAULT now(),
    ativo                  BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE INDEX idx_lic_inst_reg_licenca_ativo ON licenca_instalacao_registrada(licenca_id, ativo);

CREATE UNIQUE INDEX uq_lic_inst_reg_maquina_ativa
    ON licenca_instalacao_registrada(licenca_id, identificador_maquina)
    WHERE ativo = TRUE;

-- ---------------------------------------------------------
-- LogOperacao (auditoria genérica)
-- ---------------------------------------------------------
CREATE TABLE log_operacao (
    id               UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    entidade         VARCHAR(100) NOT NULL,
    id_registro      UUID         NOT NULL,
    operacao         CHAR(1)      NOT NULL CHECK (operacao IN ('I', 'U', 'D')),
    data_hora        TIMESTAMP    NOT NULL DEFAULT now(),
    id_usuario       UUID         REFERENCES usuario(id),
    campos_alterados JSONB
);

CREATE INDEX idx_log_operacao_entidade_registro ON log_operacao(entidade, id_registro);
CREATE INDEX idx_log_operacao_data_hora         ON log_operacao(data_hora);
