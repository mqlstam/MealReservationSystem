using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Application.Common.Interfaces.GraphQL;
using WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add core services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add WebApi specific services
builder.Services.AddScoped<IGraphQLService, GraphQLService>();
builder.Services.AddGraphQLServer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins",
        policy => policy
            .WithOrigins("https://meal-reservation-avans.azurewebsites.net")
            .AllowAnyMethod()
            .AllowAnyHeader());
});

var app = builder.Build();

// Configure GraphQL
var graphQLService = app.Services.GetRequiredService<IGraphQLService>();
graphQLService.RegisterTypes(app.Services);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

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
        var context = services.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

app.Run();
