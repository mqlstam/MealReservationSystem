// Infrastructure/Persistence/Configurations/PackageConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Entities;

public class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        builder.Property(p => p.IsAdultOnly)
            .HasColumnName("IsAdultOnly")
            .IsRequired();  // Add this line

        // Configure the relationship with Products and the join table
        builder.HasMany(p => p.Products)
            .WithMany(p => p.Packages)
            .UsingEntity(
                "PackageProduct",
                l => l.HasOne(typeof(Product)).WithMany().HasForeignKey("ProductsId"),
                r => r.HasOne(typeof(Package)).WithMany().HasForeignKey("PackagesId"),
                j => j.HasKey("PackagesId", "ProductsId"));
    }
}