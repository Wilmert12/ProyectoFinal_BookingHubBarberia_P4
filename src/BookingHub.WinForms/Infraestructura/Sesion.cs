using BookingHub.WinForms.Models;
using System.Net;
using System.Net.Http.Headers;

namespace BookingHub.WinForms.Infraestructura;

public class Sesion
{
    public SesionDto? Actual { get; private set; }

    public bool EsAdmin => Actual?.Rol == "Admin";

    public bool EsPersonal => Actual?.Rol is "Admin" or "Recepcion";

    /// <summary>True desde que la API rechazó el token o pasó la hora de vencimiento.</summary>
    public bool Expirada { get; private set; }

    /// <summary>Se dispara una sola vez cuando la sesión vence. Puede llegar desde otro hilo.</summary>
    public event Action? Expiro;

    public bool YaVencio => Actual is { } actual && DateTime.UtcNow >= actual.Expira.ToUniversalTime();

    public void Iniciar(SesionDto sesion)
    {
        Actual = sesion;
        Expirada = false;
    }

    public void Cerrar()
    {
        Actual = null;
        Expirada = false;
    }

    public void MarcarExpirada()
    {
        if (Actual is null || Expirada)
            return;

        Expirada = true;
        Expiro?.Invoke();
    }
}

public class AutenticacionHandler(Sesion sesion) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (sesion.Actual is { } actual)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actual.Token);

        var respuesta = await base.SendAsync(request, cancellationToken);

        // Un 401 con sesión abierta significa que el token venció o dejó de ser válido.
        var esLogin = request.RequestUri?.AbsolutePath.EndsWith("/api/Auth/login", StringComparison.OrdinalIgnoreCase) == true;
        if (respuesta.StatusCode == HttpStatusCode.Unauthorized && sesion.Actual is not null && !esLogin)
            sesion.MarcarExpirada();

        return respuesta;
    }
}
