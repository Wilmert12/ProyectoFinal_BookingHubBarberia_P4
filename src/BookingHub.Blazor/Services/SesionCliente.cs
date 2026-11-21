namespace BookingHub.Blazor.Services;

/// <summary>Número de cliente y teléfono que el cliente escribió en esta visita.</summary>
public class SesionCliente
{
    public int? ClienteId { get; set; }
    public string? Telefono { get; set; }
}
