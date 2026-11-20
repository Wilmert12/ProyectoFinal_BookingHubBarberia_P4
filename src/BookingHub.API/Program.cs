using BookingHub.API.Seguridad;
using BookingHub.API;
using BookingHub.Application;
using BookingHub.Application.Services;
using BookingHub.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Scalar.AspNetCore;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("BookingHubConnection");

builder.Services.AddInfrastructure(connectionString);

builder.Services.AddApplication();
builder.Services.AddOptions<ReglasReservaOptions>()
    .Bind(builder.Configuration.GetSection(ReglasReservaOptions.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.Configure<InformacionBarberiaOptions>(builder.Configuration.GetSection(InformacionBarberiaOptions.Seccion));
builder.Services.AddScoped<ServicioTokensAuth>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<FotoPerfilAzureService>();
builder.Services.AddSingleton<ICorreoTransaccional, CorreoTransaccional>();

builder.Services.AddControllers(options =>
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddOpenApi();

var origenesPermitidos = builder.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>()
    ?? ["https://localhost:7200", "http://localhost:5200"];
if (builder.Environment.IsProduction())
{
    origenesPermitidos = origenesPermitidos
        .Where(origen => Uri.TryCreate(origen, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps &&
            !uri.IsLoopback && !uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
    if (origenesPermitidos.Length == 0 || origenesPermitidos.Any(origen => origen == "*"))
        throw new InvalidOperationException("Configura Cors:OrigenesPermitidos con los dominios HTTPS de producción.");
}

builder.Services.AddCors(options =>
    options.AddPolicy("BookingHubClientes", policy =>
        policy.WithOrigins(origenesPermitidos)
            .AllowAnyHeader()
            .AllowAnyMethod()));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth-login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconocida",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("auth-register", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "ip-desconocida",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var claveJwt = builder.Configuration["Jwt:Clave"];
if (string.IsNullOrWhiteSpace(claveJwt))
{
    if (builder.Environment.IsProduction())
        throw new InvalidOperationException("Configura Jwt:Clave como secreto de aplicación en producción.");
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new
        {
            title = "Error interno",
            status = StatusCodes.Status500InternalServerError,
            detail = "Ocurrió un error interno. Inténtalo de nuevo más tarde."
        });
    }));
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers["Cache-Control"] = "no-store";
        return Task.CompletedTask;
    });
    await next();
});

app.UseHttpsRedirection();
app.UseCors("BookingHubClientes");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
