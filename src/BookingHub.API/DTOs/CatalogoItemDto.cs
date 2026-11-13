namespace BookingHub.API.DTOs;

public record CatalogoItemDto
{
    public int CatalogoItemId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool EsServicio { get; set; }
    public decimal Precio { get; set; }
    public int? DuracionMin { get; set; }
    public bool Activo { get; set; } = true;
}
