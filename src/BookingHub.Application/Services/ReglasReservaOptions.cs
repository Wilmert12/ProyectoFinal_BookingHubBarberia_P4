namespace BookingHub.Application.Services;

using System.ComponentModel.DataAnnotations;

public sealed class ReglasReservaOptions
{
    public const string Seccion = "Reservas";

    [Range(0, 168)]
    public int AnticipacionMinimaHoras { get; set; } = 2;
    [Range(1, 365)]
    public int DiasMaximosAnticipacion { get; set; } = 90;
    [Range(1, 20)]
    public int CitasActivasMaximas { get; set; } = 3;
    [Range(0, 168)]
    public int HorasMinimasParaCancelar { get; set; } = 6;
}
