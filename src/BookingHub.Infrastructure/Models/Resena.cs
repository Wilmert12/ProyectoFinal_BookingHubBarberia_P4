namespace BookingHub.Infrastructure.Models;

public partial class Resena
{
    public int ResenaId { get; set; }
    public int CitaId { get; set; }
    public int ClienteId { get; set; }
    public int BarberoId { get; set; }
    public byte Puntaje { get; set; }
    public string? Comentario { get; set; }
    public DateTime FechaRegistro { get; set; }
    public virtual Cita Cita { get; set; } = null!;
    public virtual Cliente Cliente { get; set; } = null!;
    public virtual Barbero Barbero { get; set; } = null!;
}
