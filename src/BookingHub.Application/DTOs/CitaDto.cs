namespace BookingHub.Application.DTOs;

public record CitaDto
{
    public int CitaId { get; set; }
    public int ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public int BarberoId { get; set; }
    public string? BarberoNombre { get; set; }
    public int EstadoCitaId { get; set; }
    public string? Estado { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public decimal Total { get; set; }
    /// <summary>Suma de los pagos registrados para la cita (solo lectura; lo calcula la API).</summary>
    public decimal Pagado { get; set; }
    public string? Notas { get; set; }
    public List<CitaDetalleDto>? Detalles { get; set; }
}
