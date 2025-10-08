using Ledger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure
{
    /// <summary>
    /// Contexto EF Core para persistir lançamentos e mensagens da outbox.
    /// </summary>
    public class LedgerDbContext : DbContext
    {
        public LedgerDbContext(DbContextOptions<LedgerDbContext> options) : base(options)
        {
        }

        public DbSet<LedgerEntry> Entries => Set<LedgerEntry>();
        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("public");

            modelBuilder.Entity<LedgerEntry>(builder =>
            {
                builder.HasKey(e => e.Id);
                builder.Property(e => e.MerchantId).IsRequired();
                builder.Property(e => e.Amount).HasColumnType("numeric(18,2)");
            });

            modelBuilder.Entity<OutboxMessage>(builder =>
            {
                builder.HasKey(o => o.Id);
                builder.Property(o => o.Type).IsRequired();
                builder.Property(o => o.Content).IsRequired();
                builder.Property(o => o.CreatedAt);
                builder.Property(o => o.ProcessedAt);
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}