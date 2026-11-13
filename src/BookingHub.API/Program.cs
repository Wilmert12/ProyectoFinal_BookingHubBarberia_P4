using BookingHub.API.Services;
using BookingHub.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("BookingHubConnection");

builder.Services.AddInfrastructure(connectionString);

builder.Services.AddScoped<CitaService>();
builder.Services.AddScoped<BarberoService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<CatalogoItemService>();

builder.Services.AddControllers(options =>
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddOpenApi();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
