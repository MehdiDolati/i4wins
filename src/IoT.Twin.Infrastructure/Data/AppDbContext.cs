using IoT.Twin.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IoT.Twin.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Reading> Readings => Set<Reading>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Reading>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.DeviceId, e.Metric, e.SequenceNumber }).IsUnique();
            entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Metric).IsRequired().HasMaxLength(50);
        });
    }
}
