using Models;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class CoreDbContext(CoreDbSettings settings) : DbContext
    {
        private readonly CoreDbSettings _settings = settings;

        public DbSet<HistoryModel> History { get; set; }
        public DbSet<LongRunningOperationModel> LongRunningOperation { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: true);
            optionsBuilder.UseInMemoryDatabase("dummy");
            // TODO: revert on the below
            // optionsBuilder.UseSqlServer(_settings.CoreConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<HistoryModel>().HasKey(c => c.Id);
            modelBuilder.Entity<HistoryModel>().Property(c => c.Query);
            modelBuilder.Entity<HistoryModel>().Property(c => c.Created);
            modelBuilder.Entity<HistoryModel>().Property(c => c.ResultLink);
            modelBuilder.Entity<HistoryModel>().Property(c => c.UserName);
            modelBuilder.Entity<HistoryModel>().Property(c => c.Duration);
            modelBuilder.Entity<HistoryModel>().Property(c => c.Completed);
            modelBuilder.Entity<HistoryModel>().Property(c => c.SentAt);

            modelBuilder.Entity<LongRunningOperationModel>().HasKey(c => c.Id);
        }
    }
}
