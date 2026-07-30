-- =========================================================
-- V003 - Tabelas de token HMAC por licença e nonces anti-replay
-- =========================================================

-- ---------------------------------------------------------
-- licenca_token — token HMAC-SHA256 vinculado a uma licença
-- ---------------------------------------------------------
-- Apenas o hash do segredo é armazenado; o valor em texto
-- é exibido uma única vez na emissão e nunca persistido.
CREATE TABLE IF NOT EXISTS licenca_token (
    id                UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    id_licenca        UUID         NOT NULL REFERENCES licenca(id),
    segredo_hash      VARCHAR(300) NOT NULL,
    expiracao_minutos INTEGER      NOT NULL CHECK (expiracao_minutos > 0),
    criado_em         TIMESTAMP    NOT NULL DEFAULT now(),
    ativo             BOOLEAN      NOT NULL DEFAULT TRUE
);

-- Garante no máximo um token ativo por licença
CREATE UNIQUE INDEX IF NOT EXISTS uq_licenca_token_ativo
    ON licenca_token(id_licenca)
    WHERE ativo = TRUE;

CREATE INDEX IF NOT EXISTS idx_licenca_token_id_licenca
    ON licenca_token(id_licenca);

-- ---------------------------------------------------------
-- nonce_replay — registro de nonces para proteção anti-replay
-- ---------------------------------------------------------
-- Cada nonce é válido dentro da janela de tempo configurada
-- (padrão ±5 min). Registros expirados podem ser removidos
-- periodicamente via job de manutenção.
CREATE TABLE IF NOT EXISTS nonce_replay (
    nonce      VARCHAR(128) PRIMARY KEY,
    expira_em  TIMESTAMP    NOT NULL
);

-- Índice para limpeza eficiente de nonces expirados
CREATE INDEX IF NOT EXISTS idx_nonce_replay_expira_em
    ON nonce_replay(expira_em);
