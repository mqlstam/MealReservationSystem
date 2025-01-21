using Domain.Entities;
using HotChocolate.Types;

namespace WebApi.GraphQL.Types;

public class PackageType : ObjectType<Package>
{
    protected override void Configure(IObjectTypeDescriptor<Package> descriptor)
    {
        descriptor.Field(p => p.Id);
        descriptor.Field(p => p.Name);
        descriptor.Field(p => p.City);
        descriptor.Field(p => p.CafeteriaLocation);
        descriptor.Field(p => p.PickupDateTime);
        descriptor.Field(p => p.LastReservationDateTime);
        descriptor.Field(p => p.IsAdultOnly);
        descriptor.Field(p => p.Price);
        descriptor.Field(p => p.MealType);
        
        descriptor.Field("exampleProducts")
            .ResolveWith<PackageResolvers>(r => r.GetProductNames(default!));
            
        descriptor.Field("isReserved")
            .ResolveWith<PackageResolvers>(r => r.IsReserved(default!));
    }
}

public class PackageResolvers
{
    public IEnumerable<string> GetProductNames([Parent] Package package)
    {
        return package.Products.Select(p => p.Name);
    }

    public bool IsReserved([Parent] Package package)
    {
        return package.Reservation != null;
    }
}
