using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimescaleDb_POC.Data;

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

        // GET: api/ErrorLogStats
        [HttpGet]
        public async Task<ActionResult<IEnumerable<HourlyStatsResult>>> GetHourlyStats(
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime,
            [FromQuery] string? exceptionType)
        {
            var query = @"
                SELECT 
                    bucket,
                    exception_type,
                    error_count
                FROM error_log_hourly
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
    }

    // Classe para mapear os resultados da consulta
    public class HourlyStatsResult
    {
        public DateTime Bucket { get; set; }
        public string ExceptionType { get; set; } = string.Empty;
        public long ErrorCount { get; set; }
    }
}
