namespace BookingHub.Application.DTOs;

public record PagoDto
{
    public int PagoId { get; set; }
    public int CitaId { get; set; }
    public string? ClienteNombre { get; set; }
    public string? BarberoNombre { get; set; }
    public int MetodoPagoId { get; set; }
    public string? MetodoPago { get; set; }
    public decimal Monto { get; set; }
    public decimal Propina { get; set; }
    public string? Referencia { get; set; }
    public DateTime FechaPago { get; set; }
}
