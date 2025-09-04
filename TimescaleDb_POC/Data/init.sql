CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Criar a tabela error_log
CREATE TABLE IF NOT EXISTS error_log (
    time            TIMESTAMPTZ       NOT NULL,
    message         TEXT              NOT NULL,
    exception_type  TEXT              NOT NULL
);

-- Converter a tabela para uma hypertable do TimescaleDB
SELECT create_hypertable('error_log', 'time', if_not_exists => TRUE);

-- Criar índices para melhor performance
CREATE INDEX IF NOT EXISTS idx_error_log_exception_type ON error_log (exception_type);
CREATE INDEX IF NOT EXISTS idx_error_log_time_exception_type ON error_log (time DESC, exception_type);

-- Criar continuous aggregate para contagem por hora por tipo de exceção
CREATE MATERIALIZED VIEW IF NOT EXISTS error_log_hourly_stats
WITH (timescaledb.continuous) AS
SELECT 
    time_bucket('1 hour', time) AS bucket,
    exception_type,
    COUNT(*) AS error_count
FROM error_log
GROUP BY bucket, exception_type
WITH NO DATA;

-- Criar índices para o continuous aggregate
CREATE INDEX IF NOT EXISTS idx_error_log_hourly_stats_bucket ON error_log_hourly_stats (bucket DESC);
CREATE INDEX IF NOT EXISTS idx_error_log_hourly_stats_exception_type ON error_log_hourly_stats (exception_type);
CREATE INDEX IF NOT EXISTS idx_error_log_hourly_stats_bucket_exception_type ON error_log_hourly_stats (bucket DESC, exception_type);

-- Adicionar policy para refresh automático do continuous aggregate
-- Esta policy irá atualizar o aggregate a cada hora
SELECT add_continuous_aggregate_policy('error_log_hourly_stats',
    start_offset => INTERVAL '3 hours',
    end_offset => INTERVAL '1 hour',
    schedule_interval => INTERVAL '1 hour');

-- Inserir alguns dados de exemplo
INSERT INTO error_log (time, message, exception_type) VALUES
    (NOW() - INTERVAL '1 hour', 'Erro de conexão com banco de dados', 'DatabaseConnectionException'),
    (NOW() - INTERVAL '30 minutes', 'Timeout na requisição HTTP', 'HttpRequestException'),
    (NOW(), 'Erro de validação de dados', 'ValidationException')
ON CONFLICT DO NOTHING;

-- Inserir mais dados para demonstrar o continuous aggregate
INSERT INTO error_log (time, message, exception_type) VALUES
    (NOW() - INTERVAL '2 hours', 'Erro de conexão com banco de dados', 'DatabaseConnectionException'),
    (NOW() - INTERVAL '2 hours', 'Erro de timeout', 'TimeoutException'),
    (NOW() - INTERVAL '1 hour', 'Erro de validação', 'ValidationException'),
    (NOW() - INTERVAL '1 hour', 'Erro de autenticação', 'AuthenticationException'),
    (NOW() - INTERVAL '30 minutes', 'Erro de permissão', 'PermissionException'),
    (NOW() - INTERVAL '30 minutes', 'Erro de conexão', 'DatabaseConnectionException')
ON CONFLICT DO NOTHING;

-- Refresh manual inicial do continuous aggregate
SELECT
  add_continuous_aggregate_policy(
    'error_log_hourly_stats',
    start_offset => INTERVAL '1 month',
    end_offset => INTERVAL '1 day',
    schedule_interval => INTERVAL '1 hour'
);