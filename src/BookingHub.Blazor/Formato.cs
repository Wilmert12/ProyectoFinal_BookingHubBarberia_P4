using System.Globalization;

namespace BookingHub.Blazor;

public static class Formato
{
    private static readonly CultureInfo Moneda = CrearMoneda();

    // Pesos dominicanos: RD$1,250.00
    private static CultureInfo CrearMoneda()
    {
        var cultura = (CultureInfo)CultureInfo.GetCultureInfo("en-US").Clone();
        cultura.NumberFormat.CurrencySymbol = "RD$";
        cultura.NumberFormat.CurrencyPositivePattern = 0;
        cultura.NumberFormat.CurrencyNegativePattern = 1;
        return cultura;
    }

    private static readonly string[] Dias = ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];
    private static readonly string[] Meses = ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

    // Sin depender de los datos de cultura del navegador (Blazor WASM no siempre trae es-DO).
    public static string FechaLarga(DateOnly fecha) => $"{Dias[(int)fecha.DayOfWeek]} {fecha.Day} de {Meses[fecha.Month - 1]}";

    /// <summary>Hora de República Dominicana (UTC-4, sin horario de verano), sin importar la zona del navegador.</summary>
    public static DateTime Ahora => DateTime.UtcNow.AddHours(-4);

    public static DateOnly Hoy => DateOnly.FromDateTime(Ahora);

    /// <summary>Nombre del estado para mostrar al público.</summary>
    public static string Estado(string? estado) => estado switch
    {
        "EnProceso" => "En proceso",
        "NoShow" => "No asistió",
        null => "",
        _ => estado
    };

    /// <summary>Lee la descripción de un error de la API ({ code, description }).</summary>
    public static string? DescripcionError(Refit.ApiException ex)
    {
        try
        {
            using var json = System.Text.Json.JsonDocument.Parse(ex.Content ?? "");
            return json.RootElement.TryGetProperty("description", out var d) ? d.GetString() : null;
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    public static string Precio(decimal valor) => valor.ToString("C2", Moneda);

    public static bool EsErrorDeApi(Exception ex) =>
        ex is HttpRequestException or Refit.ApiException or Refit.ApiRequestException;
}
