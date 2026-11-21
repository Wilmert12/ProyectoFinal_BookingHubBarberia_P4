using BookingHub.WinForms.Models;
using Refit;
using System.Globalization;
using System.Net;
using System.Text.Json;

namespace BookingHub.WinForms.Infraestructura;

public static class Mensajes
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string Precio(decimal valor) => valor.ToString("C2", Ui.Moneda);

    public static string Fecha(DateTime fecha) => fecha.ToString("yyyy-MM-dd");

    public static string Fecha(DateOnly fecha) => fecha.ToString("yyyy-MM-dd");

    /// <summary>
    /// Mientras la sesión está vencida no se muestran los 401 sueltos de cada módulo:
    /// la ventana principal avisa una sola vez y vuelve al login.
    /// </summary>
    public static bool SesionVencida { get; set; }

    public static void Error(IWin32Window? ventana, Exception ex)
    {
        if (SesionVencida && ex is ApiException { StatusCode: HttpStatusCode.Unauthorized })
            return;

        MessageBox.Show(ventana, Describir(ex), Ui.Marca, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public static void Informacion(IWin32Window? ventana, string mensaje)
    {
        MessageBox.Show(ventana, mensaje, Ui.Marca, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public static bool Confirmar(IWin32Window? ventana, string mensaje)
    {
        return MessageBox.Show(ventana, mensaje, Ui.Marca, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    }

    public static string Describir(Exception ex) => ex switch
    {
        ApiException api when api.StatusCode == HttpStatusCode.Unauthorized && !string.IsNullOrWhiteSpace(LeerDescripcion(api.Content)) => LeerDescripcion(api.Content)!,
        ApiException api when api.StatusCode == HttpStatusCode.Unauthorized => "Tu sesión expiró o no es válida. Cierra sesión y vuelve a entrar.",
        ApiException api when api.StatusCode == HttpStatusCode.Forbidden => "Tu usuario no tiene permiso para esta acción.",
        ApiException api when api.StatusCode == HttpStatusCode.NotFound => "El registro ya no existe. Actualiza la lista.",
        ApiException api => LeerDescripcion(api.Content) ?? $"La API respondió con un error ({(int)api.StatusCode}).",
        ApiRequestException or HttpRequestException => "No se pudo conectar con la API. Verifica que esté en ejecución y la dirección en appsettings.json.",
        TaskCanceledException => "La API tardó demasiado en responder. Intenta de nuevo.",
        _ => ex.Message
    };

    private static string? LeerDescripcion(string? contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            return null;

        try
        {
            using var documento = JsonDocument.Parse(contenido);
            var raiz = documento.RootElement;
            if (raiz.ValueKind == JsonValueKind.String)
                return raiz.GetString();
            if (raiz.TryGetProperty("description", out var descripcion))
                return descripcion.GetString();
            if (raiz.TryGetProperty("errors", out var errores))
                return string.Join("\n", errores.EnumerateObject().SelectMany(e => e.Value.EnumerateArray().Select(v => v.GetString())));
            if (raiz.TryGetProperty("title", out var titulo))
                return titulo.GetString();
        }
        catch (JsonException)
        {
            return contenido.Length > 300 ? contenido[..300] : contenido;
        }

        return null;
    }
}
