namespace BookingHub.Infrastructure.Models;

/// <summary>Token de renovación o recuperación; solo se almacena su hash.</summary>
public partial class AuthToken
{
    public long AuthTokenId { get; set; }

    public int? ClienteId { get; set; }

    public int? UsuarioId { get; set; }

    public string Tipo { get; set; } = null!;

    public string TokenHash { get; set; } = null!;

    public DateTime FechaCreacion { get; set; }

    public DateTime FechaExpiracion { get; set; }

    public DateTime? FechaRevocacion { get; set; }

    public string? ReemplazadoPorHash { get; set; }

    public virtual Cliente? Cliente { get; set; }

    public virtual Usuario? Usuario { get; set; }
}
