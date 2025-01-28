using Application.Common.Interfaces;
using Application.Common.Interfaces.Services;
using Application.Services;
using Application.Services.Mapping;
using Application.Services.NoShow;
using Infrastructure;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Services.Identity;
using WebApi.GraphQL;
using WebApi.GraphQL.Types;
using Microsoft.EntityFrameworkCore;
using HotChocolate.Execution;

var builder = WebApplication.CreateBuilder(args);

// Add core services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Configure GraphQL with error handling
builder.Services
    .AddGraphQLServer()
    .AddQueryType()
    .AddType<PackageType>()
    .AddType<ReservationType>()
    .AddTypeExtension<MealReservationQuery>()
    .AddErrorFilter<GraphQLErrorFilter>()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = true);

// Add CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        policy => policy
            .WithOrigins("https://meal-reservation-avans.azurewebsites.net") // Update with your allowed origins
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();


    app.UseSwagger();
    app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGraphQL();

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();

public class GraphQLErrorFilter : IErrorFilter
{
    private readonly ILogger<GraphQLErrorFilter> _logger;

    public GraphQLErrorFilter(ILogger<GraphQLErrorFilter> logger)
    {
        _logger = logger;
    }

    public IError OnError(IError error)
    {
        // Log the full exception details, including the stack trace
        _logger.LogError(error.Exception, "GraphQL Error: {Message}", error.Message);

        // Customize the error message for the client.
        // You might want to return a generic message in production to avoid 
        // exposing sensitive information.
        #if DEBUG
            // In development, return the full error message including the stack trace
            return error
                .WithMessage(error.Exception?.Message ?? error.Message)
                .WithCode(error.Code);
        #else
            // In production, return a generic error message
            return error.WithMessage("An unexpected error occurred.");
        #endif
    }
}