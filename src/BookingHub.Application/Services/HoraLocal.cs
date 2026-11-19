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
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }
        return null;
    }
}
