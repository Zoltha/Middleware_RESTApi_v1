using System.Data.Entity;
using MiddlewareApp.Models;

namespace MiddlewareApp.Data
{
    /// <summary>
    /// Entity Framework database context for middleware operations
    /// </summary>
    public class SqlDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of SqlDbContext using the default connection string
        /// </summary>
        public SqlDbContext() : base("name=LogDatabase")
        {
            // Enable migrations if database doesn't exist
            Database.SetInitializer(new CreateDatabaseIfNotExists<SqlDbContext>());
        }

        /// <summary>
        /// Idempotency records for preventing duplicate operations
        /// </summary>
        public DbSet<IdempotencyRecord> IdempotencyRecords { get; set; }

        /// <summary>
        /// Configure model relationships and constraints
        /// </summary>
        /// <param name="modelBuilder">Model builder instance</param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure IdempotencyRecord
            modelBuilder.Entity<IdempotencyRecord>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<IdempotencyRecord>()
                .Property(e => e.IdempotencyKey)
                .IsRequired()
                .HasMaxLength(256);

            modelBuilder.Entity<IdempotencyRecord>()
                .HasIndex(e => e.IdempotencyKey)
                .IsUnique();

            modelBuilder.Entity<IdempotencyRecord>()
                .Property(e => e.ResourceId)
                .IsRequired()
                .HasMaxLength(100);
        }
    }
}
