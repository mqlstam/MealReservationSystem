using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IAgeVerificationService _ageVerificationService;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IAgeVerificationService ageVerificationService)
        : base(options)
    {
        _ageVerificationService = ageVerificationService;
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Package> Packages => Set<Package>();
    public DbSet<Cafeteria> Cafeterias => Set<Cafeteria>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Student> Students => Set<Student>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply the Package configuration
        modelBuilder.ApplyConfiguration(new PackageConfiguration());
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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update IsAdultOnly based on products before saving
        var packageEntries = ChangeTracker.Entries<Package>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in packageEntries)
        {
            var package = entry.Entity;
            // Force load products if not already loaded
            if (!entry.Collection(p => p.Products).IsLoaded)
            {
                await entry.Collection(p => p.Products).LoadAsync(cancellationToken);
            }
            package.UpdateIsAdultOnly();
        }


        // Check for age restrictions on reservations before saving
        var reservationEntries = ChangeTracker.Entries<Reservation>()
            .Where(e => e.State == EntityState.Added)
            .Select(e => e.Entity)
            .ToList();

        foreach (var reservation in reservationEntries)
        {
            var package = await Packages
                .Include(p => p.Products)
                .FirstOrDefaultAsync(p => p.Id == reservation.PackageId, cancellationToken);
            
            var student = await Students
                .FirstOrDefaultAsync(s => s.StudentNumber == reservation.StudentNumber, cancellationToken);

            if (package != null && student != null && !_ageVerificationService.IsStudentEligibleForPackage(student, package))
            {
                throw new InvalidOperationException("Student must be 18 or older to reserve this package");
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
    
}
