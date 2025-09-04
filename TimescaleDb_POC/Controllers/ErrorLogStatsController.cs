using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimescaleDb_POC.Data;
using System.Data;

namespace TimescaleDb_POC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ErrorLogStatsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ErrorLogStatsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ErrorLogStats/hourly
        [HttpGet("hourly")]
        public async Task<ActionResult<IEnumerable<object>>> GetHourlyStats(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime,
            [FromQuery] string? exceptionType)
        {
            var query = @"
                SELECT 
                    bucket,
                    exception_type,
                    error_count
                FROM error_log_hourly_stats
                WHERE 1=1";

            var parameters = new List<object>();
            var paramIndex = 0;

            if (startTime.HasValue)
            {
                query += $" AND bucket >= @p{paramIndex}";
                parameters.Add(startTime.Value);
                paramIndex++;
            }

            if (endTime.HasValue)
            {
                query += $" AND bucket <= @p{paramIndex}";
                parameters.Add(endTime.Value);
                paramIndex++;
            }

            if (!string.IsNullOrEmpty(exceptionType))
            {
                query += $" AND exception_type = @p{paramIndex}";
                parameters.Add(exceptionType);
                paramIndex++;
            }

            query += " ORDER BY bucket DESC, exception_type";

            var result = await _context.Database
                .SqlQueryRaw<HourlyStatsResult>(query, parameters.ToArray())
                .ToListAsync();

            return Ok(result);
        }

        // GET: api/ErrorLogStats/hourly/summary
        [HttpGet("hourly/summary")]
        public async Task<ActionResult<object>> GetHourlySummary(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime)
        {
            var query = @"
                SELECT 
                    bucket,
                    SUM(error_count) as total_errors,
                    COUNT(DISTINCT exception_type) as unique_exception_types
                FROM error_log_hourly_stats
                WHERE 1=1";

            var parameters = new List<object>();
            var paramIndex = 0;

            if (startTime.HasValue)
            {
                query += $" AND bucket >= @p{paramIndex}";
                parameters.Add(startTime.Value);
                paramIndex++;
            }

            if (endTime.HasValue)
            {
                query += $" AND bucket <= @p{paramIndex}";
                parameters.Add(endTime.Value);
                paramIndex++;
            }

            query += " GROUP BY bucket ORDER BY bucket DESC";

            var result = await _context.Database
                .SqlQueryRaw<HourlySummaryResult>(query, parameters.ToArray())
                .ToListAsync();

            return Ok(result);
        }

        // GET: api/ErrorLogStats/exception-types
        [HttpGet("exception-types")]
        public async Task<ActionResult<IEnumerable<object>>> GetExceptionTypeStats(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime)
        {
            var query = @"
                SELECT 
                    exception_type,
                    SUM(error_count) as total_errors,
                    COUNT(*) as time_periods
                FROM error_log_hourly_stats
                WHERE 1=1";

            var parameters = new List<object>();
            var paramIndex = 0;

            if (startTime.HasValue)
            {
                query += $" AND bucket >= @p{paramIndex}";
                parameters.Add(startTime.Value);
                paramIndex++;
            }

            if (endTime.HasValue)
            {
                query += $" AND bucket <= @p{paramIndex}";
                parameters.Add(endTime.Value);
                paramIndex++;
            }

            query += " GROUP BY exception_type ORDER BY total_errors DESC";

            var result = await _context.Database
                .SqlQueryRaw<ExceptionTypeStatsResult>(query, parameters.ToArray())
                .ToListAsync();

            return Ok(result);
        }

        // POST: api/ErrorLogStats/refresh
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshStats()
        {
            try
            {
                // Refresh manual do continuous aggregate
                await _context.Database.ExecuteSqlRawAsync(
                    "CALL refresh_continuous_aggregate('error_log_hourly_stats', NULL, NULL)");
                
                return Ok(new { message = "Continuous aggregate refreshed successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    // Classes para mapear os resultados das consultas
    public class HourlyStatsResult
    {
        public DateTime Bucket { get; set; }
        public string ExceptionType { get; set; } = string.Empty;
        public long ErrorCount { get; set; }
    }

    public class HourlySummaryResult
    {
        public DateTime Bucket { get; set; }
        public long TotalErrors { get; set; }
        public int UniqueExceptionTypes { get; set; }
    }

    public class ExceptionTypeStatsResult
    {
        public string ExceptionType { get; set; } = string.Empty;
        public long TotalErrors { get; set; }
        public int TimePeriods { get; set; }
    }
}
