using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public sealed class HistoryModel
    {
        public HistoryModel(Guid id, string query, DateTime created) : this(id, query, created, null)
        {
        }

        public HistoryModel(Guid id, string query, DateTime created, Uri? resultedLink)
        {
            Id = id;
            Query = query ?? throw new ArgumentNullException(nameof(query));
            Created = created;
            ResultLink = resultedLink; // can be null
        }

        public Guid Id { get; }
        public DateTime Created { get; }
        public string Query { get; }
        public Uri? ResultLink { get; }
    }

    public sealed class LongRunningOperationRequestModel(int id, string query, DateTime created)
    {
        private static string NormalizeQuery(string query) => query
            .Trim()
            .Replace(Environment.NewLine, " ")
            .Replace('\t', ' ')
            .Replace('\n', ' ')
            .Replace("  ", " ")
            .ToLowerInvariant();

        public int Id => id;
        public DateTime Created => created;
        /// <summary>
        /// Used as a key. TODO: think about better approach for cache key
        /// </summary>
        public string NormalizedQuery => NormalizeQuery(query);
        public string Query => query;
    }

    public class CoreDbContext(CoreDbSettings _settings) : DbContext
    {
        public DbSet<HistoryModel> History { get; set; }
        public DbSet<LongRunningOperationRequestModel> LongRunningOperation { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);

            optionsBuilder.EnableSensitiveDataLogging(sensitiveDataLoggingEnabled: true);
            optionsBuilder.UseSqlServer(_settings.CoreConnectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<HistoryModel>().HasKey(c => c.Id);
            modelBuilder.Entity<HistoryModel>().Property(c => c.Query);
            modelBuilder.Entity<HistoryModel>().Property(c => c.Created);
            modelBuilder.Entity<HistoryModel>().Property(c => c.ResultLink);

            modelBuilder.Entity<LongRunningOperationRequestModel>().HasKey(c => c.Id);
            modelBuilder.Entity<LongRunningOperationRequestModel>().Property(c => c.Query);
            modelBuilder.Entity<LongRunningOperationRequestModel>().Property(c => c.Created);
        }
    }
}
