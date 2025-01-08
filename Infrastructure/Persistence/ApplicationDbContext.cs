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
    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(s => s.StudentNumber);
            
            entity.HasIndex(s => s.IdentityId)
                .IsUnique();
            
            entity.HasIndex(s => s.Email)
                .IsUnique();

            entity.HasMany(s => s.Reservations)
                .WithOne(r => r.Student)
                .HasForeignKey(r => r.StudentNumber)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Package>(entity =>
        {
            entity.HasMany(p => p.Products)
                .WithMany(p => p.Packages);

            entity.HasOne(p => p.Reservation)
                .WithOne(r => r.Package)
                .HasForeignKey<Reservation>(r => r.PackageId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Cafeteria)
                .WithMany(c => c.Packages)
                .HasForeignKey(p => p.CafeteriaId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
