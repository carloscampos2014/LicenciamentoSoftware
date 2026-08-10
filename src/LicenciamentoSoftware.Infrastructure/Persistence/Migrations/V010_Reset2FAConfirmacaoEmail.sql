-- Migration V010 — Corrigir fluxo de confirmação de e-mail no reset de 2FA
-- Problema: o status inicial era 'Pendente' por default, fazendo a solicitação
-- aparecer no painel Admin antes do usuário confirmar o e-mail.
-- Solução: introduzir status 'AguardandoConfirmacao' como estado inicial.
-- A solicitação só aparece no Admin (status='Pendente') após o usuário clicar no link.

-- 1. Ampliar o CHECK para incluir o novo estado
ALTER TABLE solicitacao_reset_2fa
    DROP CONSTRAINT IF EXISTS solicitacao_reset_2fa_status_check;

ALTER TABLE solicitacao_reset_2fa
    ADD CONSTRAINT solicitacao_reset_2fa_status_check
        CHECK (status IN ('AguardandoConfirmacao','Pendente','Aprovado','Rejeitado'));

-- 2. Alterar o DEFAULT para o novo estado inicial
ALTER TABLE solicitacao_reset_2fa
    ALTER COLUMN status SET DEFAULT 'AguardandoConfirmacao';

-- 3. Atualizar registros antigos que nunca foram confirmados
--    (token não usado + status ainda Pendente + nunca foram aprovados/rejeitados)
UPDATE solicitacao_reset_2fa
   SET status = 'AguardandoConfirmacao'
 WHERE status         = 'Pendente'
   AND token_usado_em IS NULL
   AND processado_em  IS NULL;

-- 4. Recriar o índice parcial para cobrir também o novo estado inicial
DROP INDEX IF EXISTS idx_solicitacao_reset_2fa_status;

CREATE INDEX idx_solicitacao_reset_2fa_status_pendente
    ON solicitacao_reset_2fa (status)
    WHERE status IN ('AguardandoConfirmacao', 'Pendente');

COMMENT ON COLUMN solicitacao_reset_2fa.status IS
    'AguardandoConfirmacao: link de e-mail ainda não clicado; Pendente: aguardando aprovação do Admin; Aprovado/Rejeitado: processado.';
