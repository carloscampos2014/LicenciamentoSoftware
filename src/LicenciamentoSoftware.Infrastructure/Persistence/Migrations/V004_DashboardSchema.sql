-- =========================================================
-- V004 - Schema para métricas e logs do Dashboard
-- =========================================================

-- ---------------------------------------------------------
-- 1. Coluna data_ultima_validacao em licenca_instalacao_registrada
-- ---------------------------------------------------------
-- Armazena o timestamp da última vez que esta instalação
-- fez uma validação bem-sucedida (login ou heartbeat).
-- Usado para detectar "instalações adormecidas" no dashboard.
ALTER TABLE licenca_instalacao_registrada
    ADD COLUMN IF NOT EXISTS data_ultima_validacao TIMESTAMP NULL;

-- ---------------------------------------------------------
-- 2. Tabela validacao_log
-- ---------------------------------------------------------
-- Registra cada tentativa de validação (sucesso e erro)
-- para métricas operacionais e alertas no dashboard.
-- Não armazena dados sensíveis — apenas metadados da chamada.
CREATE TABLE IF NOT EXISTS validacao_log (
    id              UUID         PRIMARY KEY DEFAULT uuid_generate_v4(),
    id_licenca      UUID         NOT NULL REFERENCES licenca(id),
    tipo_operacao   VARCHAR(50)  NOT NULL,   -- login | heartbeat | logout | instalacao
    resultado       VARCHAR(20)  NOT NULL,   -- sucesso | erro
    motivo_erro     VARCHAR(200) NULL,       -- token_invalido | licenca_inativa | limite_excedido
                                             -- sessao_invalida | instalacao_invalida | replay_detectado
    ip_origem       VARCHAR(45)  NULL,       -- IPv4 ou IPv6 (pode ser NULL se não disponível)
    criado_em       TIMESTAMP    NOT NULL DEFAULT now()
);

-- Índice principal: consultas por licença (detalhe de erros)
CREATE INDEX IF NOT EXISTS idx_validacao_log_licenca
    ON validacao_log(id_licenca, criado_em DESC);

-- Índice para queries de dashboard (erros nas últimas 24h por tenant)
CREATE INDEX IF NOT EXISTS idx_validacao_log_resultado_criado
    ON validacao_log(resultado, criado_em DESC);

-- Índice para limpeza periódica de registros antigos (job futuro)
CREATE INDEX IF NOT EXISTS idx_validacao_log_criado_em
    ON validacao_log(criado_em);
