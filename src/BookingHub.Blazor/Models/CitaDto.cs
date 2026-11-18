namespace BookingHub.Blazor.Models;

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
    public List<CitaDetalleDto>? Detalles { get; set; }
}

public record CitaDetalleDto
{
    public int CatalogoItemId { get; set; }
    public string? Nombre { get; set; }
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Subtotal { get; set; }
}
