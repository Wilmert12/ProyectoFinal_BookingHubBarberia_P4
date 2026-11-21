using System.Net.Http.Headers;
using System.Text.Json;
using BookingHub.Blazor.Models;
using Microsoft.JSInterop;

namespace BookingHub.Blazor.Services;

/// <summary>Sesión del personal (barberos) en la web. Se guarda en localStorage para sobrevivir a recargas.</summary>
public class SesionUsuario(IJSRuntime js)
{
    private const string Clave = "cerquilloapp.sesion";
    // Una sola lectura compartida: el menú y la página la piden a la vez al recargar.
    private Task? carga;

    public SesionDto? Actual { get; private set; }

    public bool EsBarbero => Actual?.Rol == "Barbero";

    public event Action? Cambio;

    public Task Cargar() => carga ??= Leer();

    private async Task Leer()
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", Clave);
            var sesion = string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<SesionDto>(json);
            Actual = sesion is not null && sesion.Expira > DateTime.UtcNow ? sesion : null;
        }
        catch (Exception ex) when (ex is JSException or JsonException)
        {
            Actual = null;
        }

        Cambio?.Invoke();
    }

    public async Task Iniciar(SesionDto sesion)
    {
        Actual = sesion;
        carga = Task.CompletedTask;
        await Guardar(JsonSerializer.Serialize(sesion));
        Cambio?.Invoke();
    }

    public async Task Cerrar()
    {
        Actual = null;
        await Guardar(null);
        Cambio?.Invoke();
    }

    private async Task Guardar(string? json)
    {
        try
        {
            if (json is null)
                await js.InvokeVoidAsync("localStorage.removeItem", Clave);
            else
                await js.InvokeVoidAsync("localStorage.setItem", Clave, json);
        }
        catch (JSException)
        {
            // Sin localStorage la sesión dura solo mientras la pestaña esté abierta.
        }
    }
}

public class AutenticacionHandler(SesionUsuario sesion) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (sesion.Actual is { } actual)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", actual.Token);

        return base.SendAsync(request, cancellationToken);
    }
}
