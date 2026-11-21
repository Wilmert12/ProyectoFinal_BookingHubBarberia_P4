namespace BookingHub.Application.Services;

/// <summary>
/// ITBIS de República Dominicana. Los precios del catálogo ya lo incluyen:
/// lo que paga el cliente es el total y de ahí se separa la parte del impuesto.
/// </summary>
public static class Impuestos
{
    public const decimal TasaItbis = 0.18m;

    /// <summary>Parte de ITBIS que contiene un monto con impuesto incluido (RD$600 → RD$91.53).</summary>
    public static decimal ItbisIncluido(decimal totalConItbis) =>
        Math.Round(totalConItbis - totalConItbis / (1 + TasaItbis), 2, MidpointRounding.AwayFromZero);

    /// <summary>Monto sin ITBIS (RD$600 → RD$508.47).</summary>
    public static decimal SinItbis(decimal totalConItbis) => totalConItbis - ItbisIncluido(totalConItbis);
}
