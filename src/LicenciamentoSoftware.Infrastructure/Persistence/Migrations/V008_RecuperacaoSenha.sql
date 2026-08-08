-- Migration V008 — Recuperação de senha por e-mail (Fase 17)
-- Cria a tabela para armazenar tokens de redefinição de senha.
-- O token é armazenado como hash SHA-256 para evitar exposição em caso de vazamento do banco.

CREATE TABLE IF NOT EXISTS senha_redefinicao (
    id          UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    id_usuario  UUID        NOT NULL REFERENCES usuario(id) ON DELETE CASCADE,
    token_hash  VARCHAR(64) NOT NULL UNIQUE,   -- SHA-256 em hex do token enviado por e-mail
    expira_em   TIMESTAMPTZ NOT NULL,
    usado_em    TIMESTAMPTZ NULL,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_senha_redefinicao_token_hash
    ON senha_redefinicao (token_hash)
    WHERE usado_em IS NULL;

CREATE INDEX IF NOT EXISTS idx_senha_redefinicao_usuario
    ON senha_redefinicao (id_usuario);

COMMENT ON TABLE senha_redefinicao IS 'Tokens de redefinição de senha enviados por e-mail. Expiram em 1 hora e são de uso único.';
COMMENT ON COLUMN senha_redefinicao.token_hash IS 'SHA-256 hex do token enviado no link de e-mail. Nunca armazenar o token bruto.';
COMMENT ON COLUMN senha_redefinicao.usado_em IS 'Data/hora UTC em que o token foi utilizado. NULL = ainda válido.';
