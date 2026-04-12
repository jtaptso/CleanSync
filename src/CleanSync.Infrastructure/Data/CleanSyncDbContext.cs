using CleanSync.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CleanSync.Infrastructure.Data;

public class CleanSyncDbContext : DbContext
{
    public CleanSyncDbContext(DbContextOptions<CleanSyncDbContext> options) : base(options)
    {
    }

    public DbSet<BusinessPartner> BusinessPartners { get; set; }
    public DbSet<SyncLog> SyncLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BusinessPartner>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CardCode).IsUnique();
            entity.HasIndex(e => new { e.ExternalId, e.Source });
            entity.Property(e => e.CardCode).HasMaxLength(50);
            entity.Property(e => e.CardName).HasMaxLength(200);
            entity.Property(e => e.CardType).HasMaxLength(10);
            entity.Property(e => e.Source).HasMaxLength(50);
            entity.Property(e => e.ExternalId).HasMaxLength(100);
        });

        modelBuilder.Entity<SyncLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).HasMaxLength(50);
            entity.Property(e => e.Direction).HasMaxLength(20);
        });
    }
}