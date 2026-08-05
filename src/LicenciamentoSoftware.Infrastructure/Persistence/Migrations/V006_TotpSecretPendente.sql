-- Migration V006 — TOTP setup em duas etapas
-- Guarda o segredo TOTP provisório até o usuário confirmar com o primeiro código.
-- Quando confirmado, o valor é movido para totp_secret_hash e esta coluna é limpa.

ALTER TABLE usuario
    ADD COLUMN IF NOT EXISTS totp_secret_pendente TEXT NULL;

COMMENT ON COLUMN usuario.totp_secret_pendente IS 'Segredo TOTP provisório aguardando confirmação. Nulo quando não há setup em andamento.';
