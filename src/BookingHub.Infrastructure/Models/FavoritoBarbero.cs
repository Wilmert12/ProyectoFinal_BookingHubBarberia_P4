namespace BookingHub.Infrastructure.Models;

public partial class FavoritoBarbero
{
    public int ClienteId { get; set; }
    public int BarberoId { get; set; }
    public DateTime FechaRegistro { get; set; }
    public virtual Cliente Cliente { get; set; } = null!;
    public virtual Barbero Barbero { get; set; } = null!;
}
