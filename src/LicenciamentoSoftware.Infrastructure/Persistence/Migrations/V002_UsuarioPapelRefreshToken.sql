-- =========================================================
-- V002 - Tabela usuario_papel, coluna email em usuario
--        e tabela refresh_token
-- =========================================================

-- Adiciona coluna email à tabela usuario (necessária para login por email)
ALTER TABLE usuario ADD COLUMN IF NOT EXISTS email VARCHAR(300) NOT NULL DEFAULT '';

CREATE UNIQUE INDEX IF NOT EXISTS uq_usuario_email ON usuario(LOWER(email)) WHERE ativo = TRUE;

-- ---------------------------------------------------------
-- usuario_papel — papel do usuário por tenant
-- ---------------------------------------------------------
CREATE TABLE IF NOT EXISTS usuario_papel (
    id_usuario UUID        NOT NULL REFERENCES usuario(id),
    papel      VARCHAR(50) NOT NULL,
    PRIMARY KEY (id_usuario)
);

-- ---------------------------------------------------------
-- refresh_token — tokens rotativos armazenados como hash
-- ---------------------------------------------------------
CREATE TABLE IF NOT EXISTS refresh_token (
    id          UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    id_usuario  UUID         NOT NULL REFERENCES usuario(id),
    token_hash  VARCHAR(300) NOT NULL,
    expiracao   TIMESTAMP    NOT NULL,
    revogado    BOOLEAN      NOT NULL DEFAULT FALSE,
    criado_em   TIMESTAMP    NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX IF NOT EXISTS uq_refresh_token_hash
    ON refresh_token(token_hash);

CREATE INDEX IF NOT EXISTS idx_refresh_token_usuario
    ON refresh_token(id_usuario);
