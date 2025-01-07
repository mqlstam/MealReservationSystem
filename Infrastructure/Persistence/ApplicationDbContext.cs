using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<Cafeteria> Cafeterias => Set<Cafeteria>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Package>()
            .HasMany(p => p.Products)
            .WithMany(p => p.Packages);

        modelBuilder.Entity<Package>()
            .HasOne(p => p.Reservation)
            .WithOne(r => r.Package)
            .HasForeignKey<Reservation>(r => r.PackageId);

        modelBuilder.Entity<Package>()
            .HasOne(p => p.Cafeteria)
            .WithMany(c => c.Packages)
            .HasForeignKey(p => p.CafeteriaId);
    }
}
