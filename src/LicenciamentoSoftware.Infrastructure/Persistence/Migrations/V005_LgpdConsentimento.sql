-- Migration V005 — LGPD: consentimento do titular (Art. 7 e 8)
-- Adiciona campos de registro de consentimento à tabela usuario.

ALTER TABLE usuario
    ADD COLUMN IF NOT EXISTS lgpd_aceito      BOOLEAN      NOT NULL DEFAULT FALSE,
    ADD COLUMN IF NOT EXISTS lgpd_aceito_em   TIMESTAMPTZ  NULL,
    ADD COLUMN IF NOT EXISTS lgpd_ip_origem   VARCHAR(45)  NULL;

COMMENT ON COLUMN usuario.lgpd_aceito    IS 'Indica se o titular aceitou os Termos de Uso e a Política de Privacidade.';
COMMENT ON COLUMN usuario.lgpd_aceito_em IS 'Data e hora do aceite dos termos (UTC).';
COMMENT ON COLUMN usuario.lgpd_ip_origem IS 'Endereço IP do dispositivo no momento do aceite.';
