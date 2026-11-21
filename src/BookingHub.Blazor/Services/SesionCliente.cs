using System.Text.Json;
using Microsoft.JSInterop;

namespace BookingHub.Blazor.Services;

/// <summary>
/// Número de cliente y teléfono del cliente. Se guardan en este navegador (localStorage)
/// para no tener que escribirlos en cada visita; "No soy yo" los borra.
/// </summary>
public class SesionCliente(IJSRuntime js)
{
    private const string Clave = "cerquilloapp.cliente";
    // Una sola lectura compartida aunque varias páginas la pidan a la vez.
    private Task? carga;

    public int? ClienteId { get; private set; }
    public string? Telefono { get; private set; }

    public bool Recordado => ClienteId is not null && !string.IsNullOrWhiteSpace(Telefono);

    private sealed record Datos(int ClienteId, string Telefono);

    public Task Cargar() => carga ??= Leer();

    private async Task Leer()
    {
        try
        {
            var json = await js.InvokeAsync<string?>("localStorage.getItem", Clave);
            if (!string.IsNullOrEmpty(json) && JsonSerializer.Deserialize<Datos>(json) is { } datos)
            {
                ClienteId = datos.ClienteId;
                Telefono = datos.Telefono;
            }
        }
        catch (Exception ex) when (ex is JSException or JsonException)
        {
            ClienteId = null;
            Telefono = null;
        }
    }

    /// <summary>Guarda los datos ya verificados por la API (citas consultadas, reserva o registro).</summary>
    public async Task Recordar(int clienteId, string telefono)
    {
        ClienteId = clienteId;
        Telefono = telefono.Trim();
        carga = Task.CompletedTask;
        await Escribir(JsonSerializer.Serialize(new Datos(clienteId, Telefono)));
    }

    public async Task Olvidar()
    {
        ClienteId = null;
        Telefono = null;
        await Escribir(null);
    }

    private async Task Escribir(string? json)
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
            // Navegación privada o almacenamiento bloqueado: se usa solo en memoria.
        }
    }
}
