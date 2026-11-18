namespace BookingHub.Application.DTOs;

public record CitaDetalleDto
{
    public int CitaDetalleId { get; set; }
    public int CatalogoItemId { get; set; }
    public string? Nombre { get; set; }
    public int Cantidad { get; set; } = 1;
    public decimal PrecioUnitario { get; set; }
    public decimal Descuento { get; set; }
    public decimal Subtotal { get; set; }
}
