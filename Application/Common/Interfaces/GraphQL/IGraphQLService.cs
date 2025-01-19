namespace Application.Common.Interfaces.GraphQL;

public interface IGraphQLService
{
    void RegisterTypes(IServiceProvider serviceProvider);
}
