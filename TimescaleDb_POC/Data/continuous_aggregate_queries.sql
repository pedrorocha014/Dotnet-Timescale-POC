-- Consultas úteis para trabalhar com o Continuous Aggregate error_log_hourly_stats

-- 1. Verificar se o continuous aggregate está funcionando
SELECT 
    bucket,
    exception_type,
    error_count
FROM error_log_hourly_stats 
ORDER BY bucket DESC, exception_type
LIMIT 20;

-- 2. Estatísticas das últimas 24 horas
SELECT 
    bucket,
    exception_type,
    error_count
FROM error_log_hourly_stats 
WHERE bucket >= NOW() - INTERVAL '24 hours'
ORDER BY bucket DESC, exception_type;

-- 3. Total de erros por hora (últimas 24 horas)
SELECT 
    bucket,
    SUM(error_count) as total_errors,
    COUNT(DISTINCT exception_type) as unique_exception_types
FROM error_log_hourly_stats 
WHERE bucket >= NOW() - INTERVAL '24 hours'
GROUP BY bucket 
ORDER BY bucket DESC;

-- 4. Top 5 tipos de exceção mais comuns (últimas 24 horas)
SELECT 
    exception_type,
    SUM(error_count) as total_errors
FROM error_log_hourly_stats 
WHERE bucket >= NOW() - INTERVAL '24 hours'
GROUP BY exception_type 
ORDER BY total_errors DESC
LIMIT 5;

-- 5. Estatísticas por dia (últimos 7 dias)
SELECT 
    DATE(bucket) as day,
    SUM(error_count) as total_errors,
    COUNT(DISTINCT exception_type) as unique_exception_types,
    AVG(error_count) as avg_errors_per_hour
FROM error_log_hourly_stats 
WHERE bucket >= NOW() - INTERVAL '7 days'
GROUP BY DATE(bucket)
ORDER BY day DESC;

-- 6. Comparação hora a hora (últimas 48 horas)
SELECT 
    bucket,
    SUM(CASE WHEN exception_type = 'DatabaseConnectionException' THEN error_count ELSE 0 END) as db_connection_errors,
    SUM(CASE WHEN exception_type = 'ValidationException' THEN error_count ELSE 0 END) as validation_errors,
    SUM(CASE WHEN exception_type = 'HttpRequestException' THEN error_count ELSE 0 END) as http_errors,
    SUM(error_count) as total_errors
FROM error_log_hourly_stats 
WHERE bucket >= NOW() - INTERVAL '48 hours'
GROUP BY bucket 
ORDER BY bucket DESC;

-- 7. Detectar picos de erro (horas com mais de X erros)
SELECT 
    bucket,
    SUM(error_count) as total_errors,
    COUNT(DISTINCT exception_type) as exception_types
FROM error_log_hourly_stats 
WHERE bucket >= NOW() - INTERVAL '24 hours'
GROUP BY bucket 
HAVING SUM(error_count) > 10  -- Ajuste este valor conforme necessário
ORDER BY total_errors DESC;

-- 8. Análise de tendências por tipo de exceção
SELECT 
    exception_type,
    bucket,
    error_count,
    LAG(error_count) OVER (PARTITION BY exception_type ORDER BY bucket) as previous_hour_count,
    error_count - LAG(error_count) OVER (PARTITION BY exception_type ORDER BY bucket) as change_from_previous
FROM error_log_hourly_stats 
WHERE bucket >= NOW() - INTERVAL '24 hours'
ORDER BY exception_type, bucket DESC;

-- 9. Estatísticas por período do dia
SELECT 
    EXTRACT(HOUR FROM bucket) as hour_of_day,
    SUM(error_count) as total_errors,
    COUNT(DISTINCT exception_type) as unique_exception_types,
    AVG(error_count) as avg_errors_per_hour
FROM error_log_hourly_stats 
WHERE bucket >= NOW() - INTERVAL '7 days'
GROUP BY EXTRACT(HOUR FROM bucket)
ORDER BY hour_of_day;

-- 10. Verificar se há gaps nos dados
WITH time_series AS (
    SELECT generate_series(
        date_trunc('hour', NOW() - INTERVAL '24 hours'),
        date_trunc('hour', NOW()),
        '1 hour'::interval
    ) as expected_bucket
),
actual_data AS (
    SELECT DISTINCT bucket FROM error_log_hourly_stats 
    WHERE bucket >= NOW() - INTERVAL '24 hours'
)
SELECT 
    ts.expected_bucket,
    CASE WHEN ad.bucket IS NULL THEN 'MISSING' ELSE 'PRESENT' END as status
FROM time_series ts
LEFT JOIN actual_data ad ON ts.expected_bucket = ad.bucket
ORDER BY ts.expected_bucket;

-- 11. Performance do continuous aggregate
EXPLAIN (ANALYZE, BUFFERS) 
SELECT 
    bucket,
    exception_type,
    error_count
FROM error_log_hourly_stats 
WHERE bucket >= NOW() - INTERVAL '24 hours'
ORDER BY bucket DESC, exception_type;

-- 12. Verificar policies ativas
SELECT 
    job_id,
    proc_name,
    schedule_interval,
    max_runtime,
    next_start,
    last_run_start,
    last_run_duration,
    last_run_status
FROM timescaledb_information.jobs
WHERE proc_name LIKE '%continuous_aggregate%';

-- 13. Forçar refresh manual de um período específico
-- CALL refresh_continuous_aggregate('error_log_hourly_stats', '2024-01-15 00:00:00', '2024-01-15 23:59:59');

-- 14. Verificar tamanho da view materializada
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) as size
FROM pg_tables 
WHERE tablename = 'error_log_hourly_stats';

-- 15. Estatísticas de uso dos índices
SELECT 
    indexname,
    idx_scan,
    idx_tup_read,
    idx_tup_fetch
FROM pg_stat_user_indexes 
WHERE tablename = 'error_log_hourly_stats'
ORDER BY idx_scan DESC;
