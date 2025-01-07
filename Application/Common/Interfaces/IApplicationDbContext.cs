using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Package> Packages { get; }
    DbSet<Cafeteria> Cafeterias { get; }
    DbSet<Reservation> Reservations { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
