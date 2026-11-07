using BookingHub.API.Models;
using BookingHub.API.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

var builder = WebApplication.CreateBuilder(args);

// Leer la cadena de conexión desde appsettings.json
var connectionString = builder.Configuration.GetConnectionString("BookingHubConnection");

builder.Services.AddDbContextFactory<BookingHubContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<CitaService>();
builder.Services.AddScoped<BarberoService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<CatalogoItemService>();

builder.Services.AddControllers(options =>
    // Las propiedades de navegación (Cliente, Barbero...) no son obligatorias al enviar una cita
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options =>
    {
        // Evita el error de ciclo Cita -> Cliente -> Citas -> ...
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        // Nunca devolver el PasswordHash en las respuestas (sí se puede recibir al registrar)
        options.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                typeInfo =>
                {
                    if (typeInfo.Type != typeof(Usuario)) return;
                    foreach (var prop in typeInfo.Properties)
                        if (prop.Name.Equals(nameof(Usuario.PasswordHash), StringComparison.OrdinalIgnoreCase))
                            prop.ShouldSerialize = (_, _) => false;
                }
            }
        };
    });
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); // Interfaz visual en /scalar
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
