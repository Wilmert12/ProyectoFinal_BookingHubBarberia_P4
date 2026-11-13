namespace BookingHub.API.DTOs;

public record ClienteDto
{
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool Activo { get; set; } = true;
}
