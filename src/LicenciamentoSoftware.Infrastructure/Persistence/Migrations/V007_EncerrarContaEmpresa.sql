-- Migration V007 — Encerramento de conta de empresa (Fase 12.1)
-- Adiciona campos de controle de encerramento e exclusão programada na tabela cliente.
-- encerrado_em: quando a conta foi encerrada pelo AdministradorCliente
-- exclusao_programada_em: quando os dados serão excluídos fisicamente
--   - Exclusão padrão: encerrado_em + 90 dias
--   - Exclusão imediata: = encerrado_em (excluído na próxima execução do job diário)

ALTER TABLE cliente
    ADD COLUMN IF NOT EXISTS encerrado_em         TIMESTAMPTZ NULL,
    ADD COLUMN IF NOT EXISTS exclusao_programada_em TIMESTAMPTZ NULL;

COMMENT ON COLUMN cliente.encerrado_em           IS 'Data/hora UTC em que a conta foi encerrada. NULL enquanto ativa.';
COMMENT ON COLUMN cliente.exclusao_programada_em IS 'Data/hora UTC em que os dados serão excluídos fisicamente. NULL enquanto ativa.';

CREATE INDEX IF NOT EXISTS idx_cliente_exclusao_programada
    ON cliente (exclusao_programada_em)
    WHERE exclusao_programada_em IS NOT NULL;
