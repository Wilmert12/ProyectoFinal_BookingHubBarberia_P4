using BookingHub.WinForms.Models;
using System.Net.Http.Headers;

namespace BookingHub.WinForms.Infraestructura;

public class Sesion
{
    public SesionDto? Actual { get; private set; }

    public bool EsAdmin => Actual?.Rol == "Admin";

    public bool EsPersonal => Actual?.Rol is "Admin" or "Recepcion";

    public void Iniciar(SesionDto sesion) => Actual = sesion;

    public void Cerrar() => Actual = null;
}

public class AutenticacionHandler(Sesion sesion) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (sesion.Actual is { } actual)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actual.Token);

        return base.SendAsync(request, cancellationToken);
    }
}
