namespace BookingHub.WinForms.Infraestructura;

/// <summary>
/// ITBIS (18 %) incluido en los precios. Mismo cálculo que la API:
/// ITBIS = total - total / 1.18, redondeado a centavos. Las propinas no llevan ITBIS.
/// </summary>
public static class Impuestos
{
    public const decimal TasaItbis = 0.18m;

    public static decimal ItbisIncluido(decimal totalConItbis) =>
        Math.Round(totalConItbis - totalConItbis / (1 + TasaItbis), 2, MidpointRounding.AwayFromZero);

    public static decimal SinItbis(decimal totalConItbis) => totalConItbis - ItbisIncluido(totalConItbis);
}
