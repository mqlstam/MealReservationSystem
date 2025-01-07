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
            .ResolveWith<Package>(p => 
                p.Products.Select(prod => prod.Name).ToList());
        
        descriptor.Field("isReserved")
            .ResolveWith<Package>(p => p.Reservation != null);
    }
}
