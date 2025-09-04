using Microsoft.EntityFrameworkCore;
using TimescaleDb_POC.Models;

namespace TimescaleDb_POC.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ErrorLog> ErrorLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar a tabela error_log como uma hypertable do TimescaleDB
            modelBuilder.Entity<ErrorLog>(entity =>
            {
                entity.HasKey(e => e.Time);
                entity.Property(e => e.Time).HasColumnType("timestamptz");
                entity.Property(e => e.Message).HasColumnType("text");
                entity.Property(e => e.ExceptionType).HasColumnType("text");
            });
        }
    }
}
