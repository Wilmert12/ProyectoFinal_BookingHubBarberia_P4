namespace BookingHub.API.DTOs;

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
    public string? Notas { get; set; }
}
