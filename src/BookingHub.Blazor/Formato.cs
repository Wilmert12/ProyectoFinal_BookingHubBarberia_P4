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

    public static string Precio(decimal valor) => valor.ToString("C2", Moneda);

    public static bool EsErrorDeApi(Exception ex) =>
        ex is HttpRequestException or Refit.ApiException or Refit.ApiRequestException;
}
