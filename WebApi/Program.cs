using Infrastructure;
using WebApi.GraphQL;
using WebApi.GraphQL.Types;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Add GraphQL services
builder.Services
    .AddGraphQLServer()
    .AddQueryType()
    .AddType<PackageType>()
    .AddType<ReservationType>()
    .AddTypeExtension<MealReservationQuery>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGraphQL();

app.Run();
