using BookingHub.Application;
using BookingHub.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("BookingHubConnection");

builder.Services.AddInfrastructure(connectionString);

builder.Services.AddApplication();

builder.Services.AddControllers(options =>
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddOpenApi();

var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>()
    ?? ["https://localhost:7200", "http://localhost:5200"];

builder.Services.AddCors(options =>
    options.AddPolicy("BookingHubClientes", policy =>
        policy.WithOrigins(origenesPermitidos)
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.UseCors("BookingHubClientes");
app.MapControllers();

app.Run();
