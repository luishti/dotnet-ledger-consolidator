using Consolidator.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Consolidator.Api
{
    /// <summary>
    /// Contexto EF Core para armazenar os saldos diários consolidados.
    /// </summary>
    public class ConsolidatorDbContext : DbContext
    {
        public ConsolidatorDbContext(DbContextOptions<ConsolidatorDbContext> options) : base(options)
        {
        }

        public DbSet<DailyBalance> DailyBalances => Set<DailyBalance>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("public");

            modelBuilder.Entity<DailyBalance>(b =>
            {
                b.ToTable("DailyBalances");
                b.HasKey(x => x.Id);

                b.HasIndex(x => new { x.MerchantId, x.Date }).IsUnique();

                b.Property(x => x.TotalAmount).HasColumnType("numeric(18,2)");

                b.Property(x => x.Date)
                    .HasConversion(
                        v => v.ToDateTime(TimeOnly.MinValue),
                        v => DateOnly.FromDateTime(v))
                    .HasColumnType("date");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}