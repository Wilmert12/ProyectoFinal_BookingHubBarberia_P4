namespace BookingHub.Application.DTOs;

public record ClienteDto
{
    public int ClienteId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; } = true;
}

/// <summary>Para recuperar el número de cliente con el nombre y el teléfono del registro.</summary>
public record RecuperarNumeroDto(string Nombre, string Telefono);

public record NumeroClienteDto(int ClienteId, string Nombre);
