using BookingHub.API.Seguridad;
using BookingHub.Application;
using BookingHub.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Security.Cryptography;
using System.Text;

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

var claveJwt = builder.Configuration["Jwt:Clave"];
if (string.IsNullOrWhiteSpace(claveJwt))
{
    claveJwt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    Console.Error.WriteLine("ADVERTENCIA: falta la configuración Jwt:Clave. Se usará una clave temporal y las sesiones se perderán al reiniciar la API.");
}
else if (Encoding.UTF8.GetByteCount(claveJwt) < 32)
{
    throw new InvalidOperationException("La configuración Jwt:Clave debe tener al menos 32 caracteres.");
}

var claveFirma = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveJwt));
builder.Services.AddSingleton(new GeneradorToken(claveFirma));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = GeneradorToken.Emisor,
            ValidAudience = GeneradorToken.Emisor,
            IssuerSigningKey = claveFirma,
            NameClaimType = "name",
            RoleClaimType = "role",
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddLimiteDeIntentos();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors("BookingHubClientes");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();
