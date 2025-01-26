// Infrastructure/Persistence/Configurations/PackageConfiguration.cs

using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class PackageConfiguration : IEntityTypeConfiguration<Package>
{
    public void Configure(EntityTypeBuilder<Package> builder)
    {
        // Remove the backing field configuration
        builder.Property(p => p.IsAdultOnly)
            .HasColumnName("IsAdultOnly");

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