using BookingHub.API.Models;
using BookingHub.API.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("BookingHubConnection");

builder.Services.AddDbContextFactory<BookingHubContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddScoped<CitaService>();
builder.Services.AddScoped<BarberoService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<CatalogoItemService>();

builder.Services.AddControllers(options =>
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

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

app.MapOpenApi();
app.MapScalarApiReference();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
