-- Migration V009 — Solicitação de reset de 2FA (Fase 17 / Issue #173)
-- Fluxo: login → sem acesso ao autenticador → confirma por e-mail → Admin aprova → reset executado

CREATE TABLE IF NOT EXISTS solicitacao_reset_2fa (
    id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    id_usuario       UUID        NOT NULL REFERENCES usuario(id) ON DELETE CASCADE,

    -- Token de confirmação enviado por e-mail (SHA-256 hex)
    token_hash       VARCHAR(64) NOT NULL UNIQUE,
    token_expira_em  TIMESTAMPTZ NOT NULL,
    token_usado_em   TIMESTAMPTZ NULL,

    -- Status da solicitação após confirmação por e-mail
    -- Pendente: aguardando aprovação do Admin
    -- Aprovado: reset executado
    -- Rejeitado: admin rejeitou
    status           VARCHAR(20) NOT NULL DEFAULT 'Pendente'
                                 CHECK (status IN ('Pendente','Aprovado','Rejeitado')),

    ip_origem        VARCHAR(45) NULL,
    criado_em        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    processado_em    TIMESTAMPTZ NULL      -- quando foi aprovado ou rejeitado
);

CREATE INDEX IF NOT EXISTS idx_solicitacao_reset_2fa_usuario
    ON solicitacao_reset_2fa (id_usuario);

CREATE INDEX IF NOT EXISTS idx_solicitacao_reset_2fa_status
    ON solicitacao_reset_2fa (status)
    WHERE status = 'Pendente';

CREATE INDEX IF NOT EXISTS idx_solicitacao_reset_2fa_token
    ON solicitacao_reset_2fa (token_hash)
    WHERE token_usado_em IS NULL;

COMMENT ON TABLE solicitacao_reset_2fa IS
    'Solicitações de reset de 2FA TOTP. Criadas após confirmação por e-mail, aprovadas manualmente pelo Admin.';
