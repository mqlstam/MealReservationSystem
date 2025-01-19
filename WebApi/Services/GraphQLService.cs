using Application.Common.Interfaces.GraphQL;
using HotChocolate.Execution.Configuration;
using WebApi.GraphQL;
using WebApi.GraphQL.Types;

namespace WebApi.Services;

public class GraphQLService : IGraphQLService
{
    public void RegisterTypes(IServiceProvider serviceProvider)
    {
        var builder = serviceProvider.GetRequiredService<IRequestExecutorBuilder>();
        
        builder
            .AddQueryType()
            .AddType<PackageType>()
            .AddType<ReservationType>()
            .AddTypeExtension<MealReservationQuery>();
    }
}
