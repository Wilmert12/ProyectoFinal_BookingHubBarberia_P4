namespace BookingHub.Application.Services;

/// <summary>Hora de la barbería (República Dominicana), aunque el servidor corra en UTC.</summary>
public static class HoraLocal
{
    private static readonly TimeZoneInfo? Zona = BuscarZona();

    public static DateTime Ahora => Zona is null
        ? DateTime.Now
        : TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, Zona);

    private static TimeZoneInfo? BuscarZona()
    {
        foreach (var id in new[] { "America/Santo_Domingo", "SA Western Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                // Este sistema no conoce ese nombre de zona; se prueba el siguiente.
                continue;
            }
        }
        return null;
    }
}
