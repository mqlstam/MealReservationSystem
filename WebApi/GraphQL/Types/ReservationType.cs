using Domain.Entities;
using HotChocolate.Types;

namespace WebApi.GraphQL.Types;

public class ReservationType : ObjectType<Reservation>
{
    protected override void Configure(IObjectTypeDescriptor<Reservation> descriptor)
    {
        descriptor.Field(r => r.Id);
        descriptor.Field(r => r.StudentNumber);
        descriptor.Field(r => r.ReservationDateTime);
        descriptor.Field(r => r.IsPickedUp);
        descriptor.Field(r => r.IsNoShow);
        descriptor.Field(r => r.Package);
        descriptor.Field(r => r.PackageId);
    }
}
