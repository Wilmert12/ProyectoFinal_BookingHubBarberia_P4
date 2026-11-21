using Aplicada1.Core;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace BookingHub.API.Seguridad;

/// <summary>
/// Límite de intentos por dirección IP para frenar a quien prueba contraseñas
/// o teléfonos de clientes al azar. El personal y los barberos con sesión no tienen límite.
/// </summary>
public static class LimiteDeIntentos
{
    /// <summary>Inicio de sesión: 10 intentos por minuto por IP.</summary>
    public const string Login = "login";

    /// <summary>Lo que se verifica con el teléfono del cliente: 20 solicitudes por minuto por IP.</summary>
    public const string Clientes = "clientes";

    public static IServiceCollection AddLimiteDeIntentos(this IServiceCollection services)
    {
        // En Azure la API está detrás de un proxy: la IP real del cliente viene en X-Forwarded-For.
        services.Configure<ForwardedHeadersOptions>(opciones =>
        {
            opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            opciones.KnownIPNetworks.Clear();
            opciones.KnownProxies.Clear();
        });

        services.AddRateLimiter(opciones =>
        {
            opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            opciones.OnRejected = async (contexto, cancelacion) =>
            {
                contexto.HttpContext.Response.Headers.RetryAfter = "60";
                await contexto.HttpContext.Response.WriteAsJsonAsync(
                    new Error("DEMASIADOS_INTENTOS", "Demasiados intentos seguidos. Espera un minuto y vuelve a intentar."),
                    cancelacion);
            };

            opciones.AddPolicy(Login, http => RateLimitPartition.GetFixedWindowLimiter(
                Ip(http),
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));

            opciones.AddPolicy(Clientes, http => http.User.Identity?.IsAuthenticated == true
                ? RateLimitPartition.GetNoLimiter("con-sesion")
                : RateLimitPartition.GetFixedWindowLimiter(
                    Ip(http),
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });

        return services;
    }

    private static string Ip(HttpContext http) => http.Connection.RemoteIpAddress?.ToString() ?? "desconocida";
}
