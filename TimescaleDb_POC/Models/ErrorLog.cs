using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TimescaleDb_POC.Models
{
    [Table("error_log")]
    public class ErrorLog
    {
        [Key]
        [Column("time")]
        public DateTime Time { get; set; }

        [Required]
        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Required]
        [Column("exception_type")]
        public string ExceptionType { get; set; } = string.Empty;
    }
}
