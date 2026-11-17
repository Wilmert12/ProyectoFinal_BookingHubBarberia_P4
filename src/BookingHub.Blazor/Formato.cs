using System.Globalization;

namespace BookingHub.Blazor;

public static class Formato
{
    private static readonly CultureInfo Moneda = CultureInfo.GetCultureInfo("en-US");

    public static string Precio(decimal valor) => valor.ToString("C2", Moneda);

    public static bool EsErrorDeApi(Exception ex) =>
        ex is HttpRequestException or Refit.ApiException or Refit.ApiRequestException;
}
