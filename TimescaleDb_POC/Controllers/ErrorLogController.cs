using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TimescaleDb_POC.Data;
using TimescaleDb_POC.Models;

namespace TimescaleDb_POC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ErrorLogController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ErrorLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ErrorLog
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ErrorLog>>> GetErrorLogs()
        {
            return await _context.ErrorLogs
                .OrderByDescending(e => e.Time)
                .ToListAsync();
        }

        // GET: api/ErrorLog/5
        [HttpGet("{time}")]
        public async Task<ActionResult<ErrorLog>> GetErrorLog(DateTime time)
        {
            var errorLog = await _context.ErrorLogs.FindAsync(time);

            if (errorLog == null)
            {
                return NotFound();
            }

            return errorLog;
        }

        // POST: api/ErrorLog
        [HttpPost]
        public async Task<ActionResult<ErrorLog>> PostErrorLog(ErrorLog errorLog)
        {
            if (errorLog.Time == default)
            {
                errorLog.Time = DateTime.UtcNow;
            }

            _context.ErrorLogs.Add(errorLog);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetErrorLog), new { time = errorLog.Time }, errorLog);
        }

        // PUT: api/ErrorLog/5
        [HttpPut("{time}")]
        public async Task<IActionResult> PutErrorLog(DateTime time, ErrorLog errorLog)
        {
            if (time != errorLog.Time)
            {
                return BadRequest();
            }

            _context.Entry(errorLog).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ErrorLogExists(time))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/ErrorLog/5
        [HttpDelete("{time}")]
        public async Task<IActionResult> DeleteErrorLog(DateTime time)
        {
            var errorLog = await _context.ErrorLogs.FindAsync(time);
            if (errorLog == null)
            {
                return NotFound();
            }

            _context.ErrorLogs.Remove(errorLog);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/ErrorLog/search?exceptionType=value&startTime=value&endTime=value
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<ErrorLog>>> SearchErrorLogs(
            [FromQuery] string? exceptionType,
            [FromQuery] DateTime? startTime,
            [FromQuery] DateTime? endTime)
        {
            var query = _context.ErrorLogs.AsQueryable();

            if (!string.IsNullOrEmpty(exceptionType))
            {
                query = query.Where(e => e.ExceptionType.Contains(exceptionType));
            }

            if (startTime.HasValue)
            {
                query = query.Where(e => e.Time >= startTime.Value);
            }

            if (endTime.HasValue)
            {
                query = query.Where(e => e.Time <= endTime.Value);
            }

            return await query
                .OrderByDescending(e => e.Time)
                .ToListAsync();
        }

        private bool ErrorLogExists(DateTime time)
        {
            return _context.ErrorLogs.Any(e => e.Time == time);
        }
    }
}
