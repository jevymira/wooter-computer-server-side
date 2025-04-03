using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Model;

public partial class WootComputersSourceContext : DbContext
{
    public WootComputersSourceContext()
    {
    }

    public WootComputersSourceContext(DbContextOptions<WootComputersSourceContext> options)
        : base(options)
    {
    }

    public virtual DbSet<HardwareConfiguration> Configurations { get; set; }

    public virtual DbSet<Offer> Offers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured)
        {
            return;
        }
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json");
        IConfigurationRoot configuration = builder.Build();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<HardwareConfiguration>(entity =>
        {
            entity.ToTable("Configuration");

            entity.Property(e => e.Processor).HasMaxLength(50);

            entity.HasOne(d => d.Offer).WithMany(p => p.Configurations)
                .HasForeignKey(d => d.OfferId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Configuration_Offer");
        });

        modelBuilder.Entity<Offer>(entity =>
        {
            entity.ToTable("Offer");

            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.Condition).HasMaxLength(50);
            entity.Property(e => e.Photo)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.Title).HasMaxLength(100);
            entity.Property(e => e.Url).HasMaxLength(150);

            entity.HasAlternateKey(e => e.WootId);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
